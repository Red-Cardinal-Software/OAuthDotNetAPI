using Application.DTOs.Mfa;
using Domain.Entities.Security;

namespace Application.Interfaces.Services;

/// <summary>
/// Service interface for managing MFA configuration and setup operations.
/// Handles enrollment, verification, and administration of user MFA methods.
/// </summary>
public interface IMfaConfigurationService
{
    #region Setup and Enrollment

    /// <summary>
    /// Initiates TOTP setup for a user by generating a secret and QR code.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="accountName">Account identifier (username/email) for the authenticator app</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Setup information including QR code and secret</returns>
    Task<MfaSetupDto> StartTotpSetupAsync(Guid userId, string accountName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies TOTP setup by validating the user's first code from their authenticator app.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="verificationDto">Verification code and optional method name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Completion information including recovery codes</returns>
    Task<MfaSetupCompleteDto> VerifyTotpSetupAsync(Guid userId, VerifyMfaSetupDto verificationDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates email MFA setup for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="emailAddress">Email address to use for MFA (can be different from login email)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Setup information</returns>
    Task<EmailSetupDto> StartEmailSetupAsync(Guid userId, string emailAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies email MFA setup by validating the code sent to the user's email.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="verificationDto">Verification code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Completion information including recovery codes</returns>
    Task<MfaSetupCompleteDto> VerifyEmailSetupAsync(Guid userId, VerifyMfaSetupDto verificationDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an in-progress MFA setup that hasn't been verified yet.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="mfaType">The type of MFA setup to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a setup was cancelled</returns>
    Task<bool> CancelSetupAsync(Guid userId, MfaType mfaType, CancellationToken cancellationToken = default);

    #endregion

    #region Method Management

    /// <summary>
    /// Gets an overview of the user's MFA configuration.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>MFA overview including all methods and status</returns>
    Task<MfaOverviewDto> GetMfaOverviewAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific MFA method.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="methodId">The MFA method ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Method details or null if not found</returns>
    Task<MfaMethodDto?> GetMfaMethodAsync(Guid userId, Guid methodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing MFA method's settings.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="methodId">The MFA method ID</param>
    /// <param name="updateDto">Update information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated method information</returns>
    Task<MfaMethodDto> UpdateMfaMethodAsync(Guid userId, Guid methodId, UpdateMfaMethodDto updateDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets an MFA method as the user's default.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="methodId">The MFA method ID to set as default</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetDefaultMfaMethodAsync(Guid userId, Guid methodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables an MFA method.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="methodId">The MFA method ID</param>
    /// <param name="enabled">Whether to enable or disable the method</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetMfaMethodEnabledAsync(Guid userId, Guid methodId, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an MFA method from the user's account.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="methodId">The MFA method ID to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the method was removed</returns>
    Task<bool> RemoveMfaMethodAsync(Guid userId, Guid methodId, CancellationToken cancellationToken = default);

    #endregion

    #region Recovery Codes

    /// <summary>
    /// Generates new recovery codes for an MFA method, invalidating old unused codes.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="methodId">The MFA method ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New recovery codes</returns>
    Task<string[]> RegenerateRecoveryCodesAsync(Guid userId, Guid methodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unused recovery codes for an MFA method.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="methodId">The MFA method ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of unused recovery codes</returns>
    Task<int> GetRecoveryCodeCountAsync(Guid userId, Guid methodId, CancellationToken cancellationToken = default);

    #endregion

    #region Validation

    /// <summary>
    /// Checks if a user has any enabled MFA methods.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user has enabled MFA</returns>
    Task<bool> UserHasMfaEnabledAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a user can safely remove an MFA method.
    /// Ensures they won't be locked out of their account.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="methodId">The MFA method ID to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with any warnings</returns>
    Task<MfaRemovalValidationResult> ValidateMethodRemovalAsync(Guid userId, Guid methodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can set up a specific type of MFA.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="mfaType">The MFA type to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user can set up this MFA type</returns>
    Task<bool> CanSetupMfaTypeAsync(Guid userId, MfaType mfaType, CancellationToken cancellationToken = default);

    #endregion

    #region Administrative

    /// <summary>
    /// Gets MFA statistics for administrative purposes.
    /// </summary>
    /// <param name="organizationId">Organization ID for scoped statistics. If null, returns system-wide statistics.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>MFA statistics for the specified organization or system-wide</returns>
    Task<MfaStatisticsDto> GetMfaStatisticsAsync(Guid? organizationId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up unverified MFA methods older than the specified age.
    /// </summary>
    /// <param name="maxAge">Maximum age for unverified methods</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of methods cleaned up</returns>
    Task<int> CleanupUnverifiedMethodsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Result of validating whether an MFA method can be safely removed.
/// </summary>
public class MfaRemovalValidationResult
{
    /// <summary>
    /// Whether the method can be safely removed.
    /// </summary>
    public bool CanRemove { get; init; }

    /// <summary>
    /// Warning messages for the user.
    /// </summary>
    public string[] Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether this would leave the user with no MFA methods.
    /// </summary>
    public bool WillDisableMfa { get; init; }

    /// <summary>
    /// Number of remaining enabled methods after removal.
    /// </summary>
    public int RemainingMethodCount { get; init; }
}

/// <summary>
/// System-wide MFA statistics for administrators.
/// </summary>
public class MfaStatisticsDto
{
    /// <summary>
    /// Total number of users in the system.
    /// </summary>
    public int TotalUsers { get; init; }

    /// <summary>
    /// Number of users with at least one enabled MFA method.
    /// </summary>
    public int UsersWithMfa { get; init; }

    /// <summary>
    /// Percentage of users with MFA enabled.
    /// </summary>
    public decimal MfaAdoptionRate { get; init; }

    /// <summary>
    /// Breakdown of MFA methods by type.
    /// </summary>
    public Dictionary<MfaType, int> MethodsByType { get; init; } = new();

    /// <summary>
    /// Number of unverified MFA setups.
    /// </summary>
    public int UnverifiedSetups { get; init; }

    /// <summary>
    /// When these statistics were generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; init; }
}