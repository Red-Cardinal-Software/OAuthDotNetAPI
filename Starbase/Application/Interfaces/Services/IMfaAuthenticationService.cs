using Application.DTOs.Auth;
using Domain.Entities.Security;

namespace Application.Interfaces.Services;

/// <summary>
/// Service interface for MFA authentication operations during login flow.
/// Handles challenge creation, verification, and authentication flow integration.
/// </summary>
public interface IMfaAuthenticationService
{
    #region Challenge Management

    /// <summary>
    /// Creates an MFA challenge for a user during login.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="ipAddress">IP address of the login attempt</param>
    /// <param name="userAgent">User agent of the login attempt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>MFA challenge information</returns>
    Task<MfaChallengeDto> CreateChallengeAsync(Guid userId, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies an MFA challenge with the provided code.
    /// </summary>
    /// <param name="completeMfaDto">Challenge token and verification code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result with user information</returns>
    Task<MfaVerificationResult> VerifyMfaAsync(CompleteMfaDto completeMfaDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all active challenges for a user.
    /// Used for security events or successful authentication.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of challenges invalidated</returns>
    Task<int> InvalidateUserChallengesAsync(Guid userId, CancellationToken cancellationToken = default);

    #endregion

    #region Validation

    /// <summary>
    /// Checks if a user requires MFA for authentication.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if MFA is required</returns>
    Task<bool> RequiresMfaAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default MFA method for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Default MFA method or null if none configured</returns>
    Task<MfaMethod?> GetDefaultMfaMethodAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that an MFA challenge is still active and usable.
    /// </summary>
    /// <param name="challengeToken">The challenge token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the challenge is valid</returns>
    Task<bool> IsChallengeValidAsync(string challengeToken, CancellationToken cancellationToken = default);

    #endregion

    #region Rate Limiting

    /// <summary>
    /// Checks if a user can create new MFA challenges based on rate limiting.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user can create challenges</returns>
    Task<bool> CanCreateChallengeAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of active challenges for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of active challenges</returns>
    Task<int> GetActiveChallengeCountAsync(Guid userId, CancellationToken cancellationToken = default);

    #endregion

    #region Administrative

    /// <summary>
    /// Cleans up expired MFA challenges.
    /// Should be called periodically to maintain database hygiene.
    /// </summary>
    /// <param name="expiredBefore">Delete challenges expired before this time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of challenges cleaned up</returns>
    Task<int> CleanupExpiredChallengesAsync(DateTimeOffset? expiredBefore = null, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Result of MFA verification attempt.
/// </summary>
public class MfaVerificationResult
{
    /// <summary>
    /// Whether the verification was successful.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// The user ID associated with this verification.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// The MFA method that was used for verification.
    /// </summary>
    public Guid? MfaMethodId { get; init; }

    /// <summary>
    /// Number of verification attempts remaining for this challenge.
    /// </summary>
    public int AttemptsRemaining { get; init; }

    /// <summary>
    /// Whether this challenge has been exhausted (no more attempts allowed).
    /// </summary>
    public bool IsExhausted { get; init; }

    /// <summary>
    /// Error message if verification failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Whether a recovery code was used for this verification.
    /// </summary>
    public bool UsedRecoveryCode { get; init; }

    /// <summary>
    /// Additional verification metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates a successful verification result.
    /// </summary>
    public static MfaVerificationResult Success(Guid userId, Guid? mfaMethodId = null, bool usedRecoveryCode = false)
    {
        return new MfaVerificationResult
        {
            IsValid = true,
            UserId = userId,
            MfaMethodId = mfaMethodId,
            AttemptsRemaining = 0,
            IsExhausted = false,
            UsedRecoveryCode = usedRecoveryCode
        };
    }

    /// <summary>
    /// Creates a failed verification result.
    /// </summary>
    public static MfaVerificationResult Failure(string errorMessage, int attemptsRemaining = 0, bool isExhausted = false)
    {
        return new MfaVerificationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            AttemptsRemaining = attemptsRemaining,
            IsExhausted = isExhausted
        };
    }
}