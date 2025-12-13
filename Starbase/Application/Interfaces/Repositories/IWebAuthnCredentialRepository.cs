using Domain.Entities.Security;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing WebAuthn credentials.
/// Provides data access methods for FIDO2/WebAuthn credential storage and retrieval.
/// </summary>
public interface IWebAuthnCredentialRepository
{
    /// <summary>
    /// Gets a WebAuthn credential by its unique identifier.
    /// </summary>
    /// <param name="id">The credential ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The WebAuthn credential if found, otherwise null</returns>
    Task<WebAuthnCredential?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a WebAuthn credential by its credential ID.
    /// </summary>
    /// <param name="credentialId">The base64URL-encoded credential ID from the authenticator</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The WebAuthn credential if found, otherwise null</returns>
    Task<WebAuthnCredential?> GetByCredentialIdAsync(string credentialId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active WebAuthn credentials for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active WebAuthn credentials</returns>
    Task<IReadOnlyList<WebAuthnCredential>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all WebAuthn credentials for a specific MFA method.
    /// </summary>
    /// <param name="mfaMethodId">The MFA method ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of WebAuthn credentials</returns>
    Task<IReadOnlyList<WebAuthnCredential>> GetByMfaMethodIdAsync(Guid mfaMethodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active credentials for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of active credentials</returns>
    Task<int> GetActiveCredentialCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a credential ID is already registered.
    /// </summary>
    /// <param name="credentialId">The credential ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the credential ID exists</returns>
    Task<bool> CredentialExistsAsync(string credentialId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets credentials that haven't been used for a specified time period.
    /// Used for cleanup and security monitoring.
    /// </summary>
    /// <param name="unusedSince">Find credentials not used since this time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of unused credentials</returns>
    Task<IReadOnlyList<WebAuthnCredential>> GetUnusedCredentialsAsync(DateTimeOffset unusedSince, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new WebAuthn credential to the repository.
    /// </summary>
    /// <param name="credential">The WebAuthn credential to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a WebAuthn credential from the repository.
    /// </summary>
    /// <param name="credential">The WebAuthn credential to remove</param>
    void Remove(WebAuthnCredential credential);

    /// <summary>
    /// Removes all credentials for a specific MFA method.
    /// Used when an MFA method is deleted.
    /// </summary>
    /// <param name="mfaMethodId">The MFA method ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of credentials removed</returns>
    Task<int> RemoveByMfaMethodIdAsync(Guid mfaMethodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates old or unused credentials for security maintenance.
    /// </summary>
    /// <param name="unusedSince">Deactivate credentials not used since this time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of credentials deactivated</returns>
    Task<int> DeactivateUnusedCredentialsAsync(DateTimeOffset unusedSince, CancellationToken cancellationToken = default);
}
