//#if (UseAWS)
using System.Security.Cryptography;
using System.Text.Json;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Application.Common.Configuration;
using Application.Interfaces.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Security.SigningKey;

/// <summary>
/// Signing key provider that stores keys in AWS Secrets Manager.
/// Supports automatic key rotation with configurable policies.
/// </summary>
public class AwsSecretsManagerSigningKeyProvider : ISigningKeyProvider, IDisposable
{
    private readonly IAmazonSecretsManager _secretsManager;
    private readonly SigningKeyRotationOptions _options;
    private readonly AwsSecretsManagerOptions _awsOptions;
    private readonly ILogger<AwsSecretsManagerSigningKeyProvider> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private SigningKeySet? _cachedKeySet;
    private DateTimeOffset _cacheExpiry = DateTimeOffset.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public AwsSecretsManagerSigningKeyProvider(
        IOptions<SigningKeyRotationOptions> options,
        IOptions<AwsSecretsManagerOptions> awsOptions,
        ILogger<AwsSecretsManagerSigningKeyProvider> logger)
    {
        _options = options.Value;
        _awsOptions = awsOptions.Value;
        _logger = logger;

        var config = new AmazonSecretsManagerConfig();
        if (!string.IsNullOrEmpty(_awsOptions.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(_awsOptions.Region);
        }

        _secretsManager = new AmazonSecretsManagerClient(config);

        _logger.LogInformation(
            "AwsSecretsManagerSigningKeyProvider initialized for secret {SecretName} in region {Region}",
            _options.SecretName, _awsOptions.Region ?? "default");
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

            // Save to Secrets Manager
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
                var request = new GetSecretValueRequest { SecretId = _options.SecretName };
                var response = await _secretsManager.GetSecretValueAsync(request, cancellationToken);

                _cachedKeySet = JsonSerializer.Deserialize<SigningKeySet>(response.SecretString)
                    ?? new SigningKeySet();
            }
            catch (ResourceNotFoundException)
            {
                _logger.LogInformation("No existing key set found in Secrets Manager. Will initialize on first rotation.");
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

        try
        {
            // Try to update existing secret
            var updateRequest = new PutSecretValueRequest
            {
                SecretId = _options.SecretName,
                SecretString = json
            };
            await _secretsManager.PutSecretValueAsync(updateRequest, cancellationToken);
        }
        catch (ResourceNotFoundException)
        {
            // Create new secret if it doesn't exist
            var createRequest = new CreateSecretRequest
            {
                Name = _options.SecretName,
                SecretString = json,
                Description = "JWT signing keys for application authentication"
            };
            await _secretsManager.CreateSecretAsync(createRequest, cancellationToken);
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
        _secretsManager.Dispose();
    }
}

/// <summary>
/// Configuration options for AWS Secrets Manager.
/// </summary>
public class AwsSecretsManagerOptions
{
    public const string SectionName = "AwsSecretsManager";

    /// <summary>
    /// AWS region for Secrets Manager (e.g., us-east-1).
    /// If not specified, uses the default region from environment/credentials.
    /// </summary>
    public string? Region { get; set; }
}
//#endif