using Microsoft.IdentityModel.Tokens;

namespace Application.Interfaces.Providers;

/// <summary>
/// Provides signing keys for JWT token operations with support for key rotation.
/// Implementations manage key lifecycle including rotation, caching, and multi-key validation.
/// </summary>
public interface ISigningKeyProvider
{
    /// <summary>
    /// Gets the current primary signing key for creating new tokens.
    /// This is the most recent active key used for signing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The current signing key and its identifier</returns>
    Task<SigningKeyInfo> GetCurrentSigningKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all valid keys for token validation.
    /// Includes the current key plus any recently rotated keys still within their validation window.
    /// This allows tokens signed with older keys to remain valid during rotation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all valid signing keys</returns>
    Task<IReadOnlyList<SigningKeyInfo>> GetValidationKeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates to a new signing key.
    /// The old key remains valid for token validation during the overlap window.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The new signing key info</returns>
    Task<SigningKeyInfo> RotateKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if key rotation is due based on the configured rotation policy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the current key should be rotated</returns>
    Task<bool> IsRotationDueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces a refresh of cached keys from the underlying store.
    /// Useful when keys may have been rotated by another instance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RefreshKeysAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a signing key including its identifier and creation time.
/// </summary>
public sealed class SigningKeyInfo
{
    /// <summary>
    /// Unique identifier for this key version.
    /// Used for key ID (kid) in JWT headers and for tracking purposes.
    /// </summary>
    public required string KeyId { get; init; }

    /// <summary>
    /// The security key used for signing and validation.
    /// </summary>
    public required SecurityKey Key { get; init; }

    /// <summary>
    /// When this key was created or activated.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When this key expires and should no longer be used for validation.
    /// Null means the key never expires (not recommended for production).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Whether this key is currently the primary key for signing new tokens.
    /// </summary>
    public bool IsPrimary { get; init; }

    /// <summary>
    /// Checks if this key is valid for token validation at the given time.
    /// </summary>
    public bool IsValidAt(DateTimeOffset timestamp) =>
        timestamp >= CreatedAt && (ExpiresAt == null || timestamp <= ExpiresAt);
}