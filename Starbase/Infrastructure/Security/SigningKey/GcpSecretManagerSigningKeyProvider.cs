//#if (UseGCP)
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Common.Configuration;
using Application.Interfaces.Providers;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Security.SigningKey;

/// <summary>
/// Signing key provider that stores keys in Google Cloud Secret Manager.
/// Supports automatic key rotation with configurable policies.
/// </summary>
public class GcpSecretManagerSigningKeyProvider : ISigningKeyProvider, IDisposable
{
    private readonly SecretManagerServiceClient _secretClient;
    private readonly SigningKeyRotationOptions _options;
    private readonly GcpSecretManagerOptions _gcpOptions;
    private readonly ILogger<GcpSecretManagerSigningKeyProvider> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private SigningKeySet? _cachedKeySet;
    private DateTimeOffset _cacheExpiry = DateTimeOffset.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public GcpSecretManagerSigningKeyProvider(
        IOptions<SigningKeyRotationOptions> options,
        IOptions<GcpSecretManagerOptions> gcpOptions,
        ILogger<GcpSecretManagerSigningKeyProvider> logger)
    {
        _options = options.Value;
        _gcpOptions = gcpOptions.Value;
        _logger = logger;

        _secretClient = SecretManagerServiceClient.Create();

        _logger.LogInformation(
            "GcpSecretManagerSigningKeyProvider initialized for project {ProjectId}, secret {SecretName}",
            _gcpOptions.ProjectId, _options.SecretName);
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

            // Save to Secret Manager
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
                var secretName = SecretVersionName.FromProjectSecretSecretVersion(
                    _gcpOptions.ProjectId, _options.SecretName, "latest");

                var response = await _secretClient.AccessSecretVersionAsync(secretName, cancellationToken);
                var secretValue = response.Payload.Data.ToStringUtf8();

                _cachedKeySet = JsonSerializer.Deserialize<SigningKeySet>(secretValue)
                    ?? new SigningKeySet();
            }
            catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
            {
                _logger.LogInformation("No existing key set found in Secret Manager. Will initialize on first rotation.");
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
        var payload = Google.Protobuf.ByteString.CopyFromUtf8(json);

        var secretName = new SecretName(_gcpOptions.ProjectId, _options.SecretName);

        try
        {
            // Try to add a new version to existing secret
            var request = new AddSecretVersionRequest
            {
                ParentAsSecretName = secretName,
                Payload = new SecretPayload { Data = payload }
            };
            await _secretClient.AddSecretVersionAsync(request, cancellationToken);
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            // Create the secret first if it doesn't exist
            var parent = new ProjectName(_gcpOptions.ProjectId);
            var createRequest = new CreateSecretRequest
            {
                ParentAsProjectName = parent,
                SecretId = _options.SecretName,
                Secret = new Secret
                {
                    Replication = new Replication
                    {
                        Automatic = new Replication.Types.Automatic()
                    }
                }
            };
            await _secretClient.CreateSecretAsync(createRequest, cancellationToken);

            // Now add the first version
            var addVersionRequest = new AddSecretVersionRequest
            {
                ParentAsSecretName = secretName,
                Payload = new SecretPayload { Data = payload }
            };
            await _secretClient.AddSecretVersionAsync(addVersionRequest, cancellationToken);
        }

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
/// Configuration options for Google Cloud Secret Manager.
/// </summary>
public class GcpSecretManagerOptions
{
    public const string SectionName = "GcpSecretManager";

    /// <summary>
    /// Google Cloud project ID.
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;
}
//#endif