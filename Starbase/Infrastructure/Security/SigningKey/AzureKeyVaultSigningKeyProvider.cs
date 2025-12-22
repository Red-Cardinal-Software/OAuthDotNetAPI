//#if (UseAzure)
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Common.Configuration;
using Application.Interfaces.Providers;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Security.SigningKey;

/// <summary>
/// Signing key provider that stores keys in Azure Key Vault.
/// Supports automatic key rotation with configurable policies.
/// </summary>
public class AzureKeyVaultSigningKeyProvider : ISigningKeyProvider, IDisposable
{
    private readonly SecretClient _secretClient;
    private readonly SigningKeyRotationOptions _options;
    private readonly ILogger<AzureKeyVaultSigningKeyProvider> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private SigningKeySet? _cachedKeySet;
    private DateTimeOffset _cacheExpiry = DateTimeOffset.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public AzureKeyVaultSigningKeyProvider(
        IOptions<SigningKeyRotationOptions> options,
        IOptions<AzureKeyVaultOptions> keyVaultOptions,
        ILogger<AzureKeyVaultSigningKeyProvider> logger)
    {
        _options = options.Value;
        _logger = logger;

        var vaultUri = new Uri(keyVaultOptions.Value.VaultUri);
        _secretClient = new SecretClient(vaultUri, new DefaultAzureCredential());

        _logger.LogInformation(
            "AzureKeyVaultSigningKeyProvider initialized for vault {VaultUri}, secret {SecretName}",
            vaultUri.Host, _options.SecretName);
    }

    public async Task<SigningKeyInfo> GetCurrentSigningKeyAsync(CancellationToken cancellationToken = default)
    {
        var keySet = await GetKeySetAsync(cancellationToken);
        var primary = keySet.Keys.FirstOrDefault(k => k.IsPrimary)
            ?? throw new InvalidOperationException("No primary signing key found. Initialize keys first.");

        return ToSigningKeyInfo(primary);
    }

    public async Task<IReadOnlyList<SigningKeyInfo>> GetValidationKeysAsync(CancellationToken cancellationToken = default)
    {
        var keySet = await GetKeySetAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        return keySet.Keys
            .Where(k => k.ExpiresAt == null || k.ExpiresAt > now)
            .Select(ToSigningKeyInfo)
            .ToList();
    }

    public async Task<SigningKeyInfo> RotateKeyAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Starting key rotation...");

            // Get current key set (force refresh)
            _cachedKeySet = null;
            var keySet = await GetKeySetAsync(cancellationToken);

            // Generate new key
            var newKeyMaterial = GenerateKeyMaterial();
            var newKeyId = GenerateKeyId();
            var now = DateTimeOffset.UtcNow;

            var newEntry = new SigningKeyEntry
            {
                KeyId = newKeyId,
                KeyMaterial = Convert.ToBase64String(newKeyMaterial),
                CreatedAt = now,
                ExpiresAt = now.AddDays(_options.RotationIntervalDays + _options.KeyOverlapWindowDays),
                IsPrimary = true
            };

            // Demote old primary and update expiry
            foreach (var key in keySet.Keys.Where(k => k.IsPrimary))
            {
                key.IsPrimary = false;
                key.ExpiresAt ??= now.AddDays(_options.KeyOverlapWindowDays);
            }

            // Add new key at the beginning
            keySet.Keys.Insert(0, newEntry);

            // Remove expired keys beyond max count
            keySet.Keys = keySet.Keys
                .Where(k => k.ExpiresAt == null || k.ExpiresAt > now)
                .Take(_options.MaximumActiveKeys)
                .ToList();

            // Save to Key Vault
            await SaveKeySetAsync(keySet, cancellationToken);

            _logger.LogInformation(
                "Key rotation complete. New key ID: {KeyId}, active keys: {ActiveKeyCount}",
                newKeyId[..8] + "...", keySet.Keys.Count);

            return ToSigningKeyInfo(newEntry);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> IsRotationDueAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return false;

        var keySet = await GetKeySetAsync(cancellationToken);
        var primary = keySet.Keys.FirstOrDefault(k => k.IsPrimary);

        if (primary == null)
        {
            _logger.LogWarning("No primary key found - rotation needed to initialize");
            return true;
        }

        var keyAge = DateTimeOffset.UtcNow - primary.CreatedAt;
        var rotationDue = keyAge.TotalDays >= _options.RotationIntervalDays;

        if (rotationDue)
        {
            _logger.LogInformation(
                "Key rotation due. Key age: {KeyAgeDays:F1} days, rotation interval: {RotationInterval} days",
                keyAge.TotalDays, _options.RotationIntervalDays);
        }

        return rotationDue;
    }

    public async Task RefreshKeysAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _cachedKeySet = null;
            _cacheExpiry = DateTimeOffset.MinValue;
            await GetKeySetAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<SigningKeySet> GetKeySetAsync(CancellationToken cancellationToken)
    {
        if (_cachedKeySet != null && DateTimeOffset.UtcNow < _cacheExpiry)
            return _cachedKeySet;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_cachedKeySet != null && DateTimeOffset.UtcNow < _cacheExpiry)
                return _cachedKeySet;

            try
            {
                var response = await _secretClient.GetSecretAsync(_options.SecretName, cancellationToken: cancellationToken);
                _cachedKeySet = JsonSerializer.Deserialize<SigningKeySet>(response.Value.Value)
                    ?? new SigningKeySet();
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogInformation("No existing key set found in Key Vault. Will initialize on first rotation.");
                _cachedKeySet = new SigningKeySet();
            }

            _cacheExpiry = DateTimeOffset.UtcNow.Add(CacheDuration);
            return _cachedKeySet;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveKeySetAsync(SigningKeySet keySet, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(keySet, new JsonSerializerOptions { WriteIndented = true });
        await _secretClient.SetSecretAsync(_options.SecretName, json, cancellationToken);

        _cachedKeySet = keySet;
        _cacheExpiry = DateTimeOffset.UtcNow.Add(CacheDuration);
    }

    private byte[] GenerateKeyMaterial()
    {
        var key = new byte[_options.KeySizeBytes];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    private static string GenerateKeyId()
    {
        return $"key-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString()[..8]}";
    }

    private static SigningKeyInfo ToSigningKeyInfo(SigningKeyEntry entry)
    {
        var keyBytes = Convert.FromBase64String(entry.KeyMaterial);
        return new SigningKeyInfo
        {
            KeyId = entry.KeyId,
            Key = new SymmetricSecurityKey(keyBytes) { KeyId = entry.KeyId },
            CreatedAt = entry.CreatedAt,
            ExpiresAt = entry.ExpiresAt,
            IsPrimary = entry.IsPrimary
        };
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}

/// <summary>
/// Configuration options for Azure Key Vault.
/// </summary>
public class AzureKeyVaultOptions
{
    public const string SectionName = "AzureKeyVault";

    /// <summary>
    /// The URI of the Azure Key Vault (e.g., https://myvault.vault.azure.net/).
    /// </summary>
    public string VaultUri { get; set; } = string.Empty;
}
//#endif