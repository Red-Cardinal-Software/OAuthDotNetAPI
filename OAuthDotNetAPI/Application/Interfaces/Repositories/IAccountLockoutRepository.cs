using Domain.Entities.Security;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing AccountLockout entities.
/// Provides data access methods for account lockout tracking and management.
/// </summary>
public interface IAccountLockoutRepository
{
    /// <summary>
    /// Gets the account lockout record for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Account lockout record if exists, null otherwise</returns>
    Task<AccountLockout?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new account lockout record to the repository.
    /// </summary>
    /// <param name="accountLockout">The account lockout record to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(AccountLockout accountLockout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing account lockout record.
    /// </summary>
    /// <param name="accountLockout">The account lockout record to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(AccountLockout accountLockout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates an account lockout record for the specified user.
    /// If no record exists, a new one will be created.
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Existing or newly created account lockout record</returns>
    Task<AccountLockout> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all accounts that are currently locked out.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of currently locked accounts</returns>
    Task<IReadOnlyList<AccountLockout>> GetActiveLockedAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets accounts whose lockout has expired and can be automatically unlocked.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of accounts with expired lockouts</returns>
    Task<IReadOnlyList<AccountLockout>> GetExpiredLockoutsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets accounts that were manually locked by a specific administrator.
    /// </summary>
    /// <param name="lockedByUserId">The ID of the user who performed the lockouts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of accounts locked by the specified user</returns>
    Task<IReadOnlyList<AccountLockout>> GetAccountsLockedByUserAsync(
        Guid lockedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets lockout statistics for reporting and monitoring.
    /// </summary>
    /// <param name="since">Calculate statistics from this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary containing lockout statistics</returns>
    Task<Dictionary<string, object>> GetLockoutStatisticsAsync(
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes account lockout records that are no longer needed
    /// (e.g., for users that have been deleted or after a retention period).
    /// </summary>
    /// <param name="userIds">User IDs whose lockout records should be deleted</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records deleted</returns>
    Task<int> DeleteByUserIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);
}