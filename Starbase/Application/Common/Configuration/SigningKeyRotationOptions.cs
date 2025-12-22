using System.ComponentModel.DataAnnotations;

namespace Application.Common.Configuration;

/// <summary>
/// Configuration options for JWT signing key rotation.
/// Controls the lifecycle of signing keys including rotation frequency and validation windows.
/// </summary>
public class SigningKeyRotationOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "SigningKeyRotation";

    /// <summary>
    /// Whether automatic key rotation is enabled.
    /// When false, keys must be rotated manually.
    /// Default: false (manual rotation for safety).
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// How often to rotate the signing key, in days.
    /// Industry recommendation: 30-90 days for high-security environments.
    /// Default: 30 days.
    /// </summary>
    [Range(1, 365, ErrorMessage = "Rotation interval must be between 1 and 365 days")]
    public int RotationIntervalDays { get; set; } = 30;

    /// <summary>
    /// How long old keys remain valid for token validation after rotation, in days.
    /// This should be longer than your longest-lived token (access + refresh).
    /// Default: 7 days (covers typical refresh token lifetimes).
    /// </summary>
    [Range(1, 90, ErrorMessage = "Key overlap window must be between 1 and 90 days")]
    public int KeyOverlapWindowDays { get; set; } = 7;

    /// <summary>
    /// Maximum number of keys to keep in the validation set.
    /// Older keys beyond this count are removed.
    /// Default: 3 (current + 2 previous versions).
    /// </summary>
    [Range(2, 10, ErrorMessage = "Maximum keys must be between 2 and 10")]
    public int MaximumActiveKeys { get; set; } = 3;

    /// <summary>
    /// How often the background service checks for rotation, in minutes.
    /// Default: 60 minutes.
    /// </summary>
    [Range(1, 1440, ErrorMessage = "Check interval must be between 1 and 1440 minutes")]
    public int CheckIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Secret name or path in the cloud secrets manager.
    /// For Azure: Key Vault secret name (e.g., "jwt-signing-keys")
    /// For AWS: Secrets Manager secret name (e.g., "myapp/jwt-signing-keys")
    /// For GCP: Secret name (e.g., "jwt-signing-keys")
    /// </summary>
    [Required(ErrorMessage = "Secret name is required when rotation is enabled")]
    public string SecretName { get; set; } = "jwt-signing-keys";

    /// <summary>
    /// Algorithm used for signing. Only symmetric algorithms supported.
    /// Default: HS256 (HMAC-SHA256).
    /// </summary>
    public string Algorithm { get; set; } = "HS256";

    /// <summary>
    /// Minimum key size in bytes for generated keys.
    /// HS256 requires at least 32 bytes (256 bits).
    /// Default: 64 bytes (512 bits) for extra security margin.
    /// </summary>
    [Range(32, 512, ErrorMessage = "Key size must be between 32 and 512 bytes")]
    public int KeySizeBytes { get; set; } = 64;
}