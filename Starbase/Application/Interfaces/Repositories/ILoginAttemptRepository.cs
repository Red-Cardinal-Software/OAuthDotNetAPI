using Domain.Entities.Security;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing LoginAttempt entities.
/// Provides data access methods for tracking and querying login attempts
/// for security monitoring and analysis purposes.
/// </summary>
public interface ILoginAttemptRepository
{
    /// <summary>
    /// Adds a new login attempt record to the repository.
    /// </summary>
    /// <param name="loginAttempt">The login attempt to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(LoginAttempt loginAttempt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent login attempts for a specific user within a time window.
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="since">Only include attempts after this timestamp</param>
    /// <param name="includeSuccessful">Whether to include successful attempts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of login attempts ordered by attempt time (newest first)</returns>
    Task<IReadOnlyList<LoginAttempt>> GetRecentAttemptsAsync(
        Guid userId,
        DateTimeOffset since,
        bool includeSuccessful = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of failed login attempts for a user within a time window.
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="since">Only count attempts after this timestamp</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of failed attempts in the time window</returns>
    Task<int> GetFailedAttemptCountAsync(
        Guid userId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent login attempts from a specific IP address.
    /// Useful for detecting potential attacks from specific sources.
    /// </summary>
    /// <param name="ipAddress">The IP address to search for</param>
    /// <param name="since">Only include attempts after this timestamp</param>
    /// <param name="includeSuccessful">Whether to include successful attempts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of login attempts from the IP address</returns>
    Task<IReadOnlyList<LoginAttempt>> GetRecentAttemptsByIpAsync(
        string ipAddress,
        DateTimeOffset since,
        bool includeSuccessful = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets login attempts for a specific username, regardless of whether
    /// the username corresponds to an existing user account.
    /// </summary>
    /// <param name="username">The username to search for</param>
    /// <param name="since">Only include attempts after this timestamp</param>
    /// <param name="includeSuccessful">Whether to include successful attempts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of login attempts for the username</returns>
    Task<IReadOnlyList<LoginAttempt>> GetAttemptsByUsernameAsync(
        string username,
        DateTimeOffset since,
        bool includeSuccessful = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes login attempt records older than the specified date.
    /// Used for cleanup and data retention compliance.
    /// </summary>
    /// <param name="olderThan">Delete records older than this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records deleted</returns>
    Task<int> DeleteOldAttemptsAsync(
        DateTimeOffset olderThan,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets login attempt statistics for security reporting.
    /// </summary>
    /// <param name="since">Calculate statistics from this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary containing various statistics</returns>
    Task<Dictionary<string, object>> GetLoginStatisticsAsync(
        DateTimeOffset since,
        CancellationToken cancellationToken = default);
}