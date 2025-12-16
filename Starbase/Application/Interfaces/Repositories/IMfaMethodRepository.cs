using Domain.Entities.Security;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing MFA methods.
/// Provides data access methods for multi-factor authentication configuration and management.
/// </summary>
public interface IMfaMethodRepository
{
    /// <summary>
    /// Gets an MFA method by its unique identifier.
    /// </summary>
    /// <param name="id">The MFA method ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The MFA method if found, otherwise null</returns>
    Task<MfaMethod?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all MFA methods for a specific user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of user's MFA methods</returns>
    Task<IReadOnlyList<MfaMethod>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all enabled MFA methods for a specific user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of user's enabled MFA methods</returns>
    Task<IReadOnlyList<MfaMethod>> GetEnabledByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific MFA method by user ID and type.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="type">The MFA type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The MFA method if found, otherwise null</returns>
    Task<MfaMethod?> GetByUserAndTypeAsync(Guid userId, MfaType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default MFA method for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The default MFA method if found, otherwise null</returns>
    Task<MfaMethod?> GetDefaultByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has any enabled MFA methods.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user has enabled MFA, otherwise false</returns>
    Task<bool> UserHasEnabledMfaAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of enabled MFA methods for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of enabled MFA methods</returns>
    Task<int> GetEnabledCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new MFA method to the repository.
    /// </summary>
    /// <param name="mfaMethod">The MFA method to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(MfaMethod mfaMethod, CancellationToken cancellationToken = default);


    /// <summary>
    /// Removes an MFA method from the repository.
    /// </summary>
    /// <param name="mfaMethod">The MFA method to remove</param>
    void Remove(MfaMethod mfaMethod);

    /// <summary>
    /// Removes all default flags from a user's MFA methods.
    /// Used when setting a new default method.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearDefaultFlagsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets MFA methods that haven't been verified within a specific time period.
    /// Used for cleanup operations.
    /// </summary>
    /// <param name="olderThan">Date threshold for unverified methods</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of unverified MFA methods</returns>
    Task<IReadOnlyList<MfaMethod>> GetUnverifiedOlderThanAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of users with at least one enabled MFA method.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of users with MFA enabled</returns>
    Task<int> GetUsersWithMfaCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of enabled MFA methods grouped by type.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary with MFA type counts</returns>
    Task<Dictionary<MfaType, int>> GetMethodCountByTypeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unverified MFA setups.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of unverified methods</returns>
    Task<int> GetUnverifiedMethodCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of users with at least one enabled MFA method in a specific organization.
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of users with MFA enabled in the organization</returns>
    Task<int> GetUsersWithMfaCountForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of enabled MFA methods grouped by type for a specific organization.
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary with MFA type counts for the organization</returns>
    Task<Dictionary<MfaType, int>> GetMethodCountByTypeForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unverified MFA setups for a specific organization.
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of unverified methods in the organization</returns>
    Task<int> GetUnverifiedMethodCountForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
