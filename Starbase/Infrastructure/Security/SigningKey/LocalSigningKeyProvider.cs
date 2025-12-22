using System.Security.Cryptography;
using System.Text;
using Application.Common.Configuration;
using Application.Interfaces.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Security.SigningKey;

/// <summary>
/// Local signing key provider for development environments.
/// Uses the JWT signing key from configuration (appsettings.json).
/// Does NOT support automatic rotation - for production, use a cloud provider.
/// </summary>
public class LocalSigningKeyProvider : ISigningKeyProvider
{
    private readonly AppOptions _appOptions;
    private readonly SigningKeyRotationOptions _rotationOptions;
    private readonly ILogger<LocalSigningKeyProvider> _logger;
    private readonly SigningKeyInfo _keyInfo;

    public LocalSigningKeyProvider(
        IOptions<AppOptions> appOptions,
        IOptions<SigningKeyRotationOptions> rotationOptions,
        ILogger<LocalSigningKeyProvider> logger)
    {
        _appOptions = appOptions.Value;
        _rotationOptions = rotationOptions.Value;
        _logger = logger;

        // Create a stable key ID based on the key content
        var keyId = ComputeKeyId(_appOptions.JwtSigningKey);

        _keyInfo = new SigningKeyInfo
        {
            KeyId = keyId,
            Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appOptions.JwtSigningKey)),
            CreatedAt = DateTimeOffset.MinValue, // Local keys don't track creation
            ExpiresAt = null, // Local keys don't expire
            IsPrimary = true
        };

        _logger.LogInformation(
            "LocalSigningKeyProvider initialized with key ID {KeyId}. " +
            "This provider does not support rotation - use a cloud provider for production",
            keyId[..8] + "...");
    }

    public Task<SigningKeyInfo> GetCurrentSigningKeyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_keyInfo);
    }

    public Task<IReadOnlyList<SigningKeyInfo>> GetValidationKeysAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<SigningKeyInfo>>(new[] { _keyInfo });
    }

    public Task<SigningKeyInfo> RotateKeyAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Key rotation requested on LocalSigningKeyProvider. " +
            "Local provider does not support rotation. " +
            "Configure a cloud provider (Azure Key Vault, AWS Secrets Manager, or GCP Secret Manager) for production use.");

        throw new NotSupportedException(
            "LocalSigningKeyProvider does not support key rotation. " +
            "Use a cloud-based provider for automatic key rotation.");
    }

    public Task<bool> IsRotationDueAsync(CancellationToken cancellationToken = default)
    {
        // Local provider never indicates rotation is due
        return Task.FromResult(false);
    }

    public Task RefreshKeysAsync(CancellationToken cancellationToken = default)
    {
        // No-op for local provider - keys come from configuration
        return Task.CompletedTask;
    }

    private static string ComputeKeyId(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToBase64String(hash)[..16].Replace('+', '-').Replace('/', '_');
    }
}