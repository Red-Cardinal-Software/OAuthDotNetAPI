using System.Security.Cryptography;

namespace Domain.Entities.Security;

/// <summary>
/// Represents a multi-factor authentication challenge issued during login.
/// Tracks the state of an MFA verification attempt with security controls.
/// </summary>
public class MfaChallenge
{
    /// <summary>
    /// Unique identifier for the MFA challenge.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The user who needs to complete this MFA challenge.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Secure token used to identify this challenge in API requests.
    /// </summary>
    public string ChallengeToken { get; private set; }

    /// <summary>
    /// The type of MFA method being challenged.
    /// </summary>
    public MfaType Type { get; private set; }

    /// <summary>
    /// Optional reference to a specific MFA method being used.
    /// </summary>
    public Guid? MfaMethodId { get; private set; }

    /// <summary>
    /// Whether this challenge has been successfully completed.
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Whether this challenge has expired or been invalidated.
    /// </summary>
    public bool IsInvalid { get; private set; }

    /// <summary>
    /// Number of failed verification attempts for this challenge.
    /// </summary>
    public int AttemptCount { get; private set; }

    /// <summary>
    /// IP address where the challenge was initiated from.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User agent string from the challenge initiation.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Additional metadata for the challenge (e.g., phone number for SMS).
    /// </summary>
    public string? Metadata { get; private set; }

    /// <summary>
    /// Timestamp when this challenge was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Timestamp when this challenge expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// Timestamp when this challenge was completed (if applicable).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Timestamp of the last failed attempt (if any).
    /// </summary>
    public DateTimeOffset? LastAttemptAt { get; private set; }

    // Configuration constants
    private const int MaxAttempts = 3;
    private const int ChallengeValidityMinutes = 5;
    private const int TokenLength = 32;

    /// <summary>
    /// Private constructor for Entity Framework Core.
    /// </summary>
    private MfaChallenge()
    {
        ChallengeToken = string.Empty;
    }

    /// <summary>
    /// Creates a new MFA challenge for a user.
    /// </summary>
    /// <param name="userId">The user ID who needs to complete MFA</param>
    /// <param name="type">The type of MFA method to use</param>
    /// <param name="mfaMethodId">Optional specific MFA method ID</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <param name="userAgent">User agent of the request</param>
    /// <returns>A new MFA challenge</returns>
    public static MfaChallenge Create(
        Guid userId,
        MfaType type,
        Guid? mfaMethodId = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        var now = DateTimeOffset.UtcNow;

        return new MfaChallenge
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ChallengeToken = GenerateSecureToken(),
            Type = type,
            MfaMethodId = mfaMethodId,
            IsCompleted = false,
            IsInvalid = false,
            AttemptCount = 0,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(ChallengeValidityMinutes)
        };
    }

    /// <summary>
    /// Records a failed verification attempt.
    /// </summary>
    /// <returns>True if more attempts are allowed, false if challenge should be invalidated</returns>
    public bool RecordFailedAttempt()
    {
        if (IsInvalid || IsCompleted)
            throw new InvalidOperationException("Cannot record attempt on invalid or completed challenge");

        AttemptCount++;
        LastAttemptAt = DateTimeOffset.UtcNow;

        if (AttemptCount >= MaxAttempts)
        {
            Invalidate();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Marks this challenge as successfully completed.
    /// </summary>
    public void Complete()
    {
        if (IsInvalid)
            throw new InvalidOperationException("Cannot complete an invalid challenge");

        if (IsCompleted)
            throw new InvalidOperationException("Challenge is already completed");

        IsCompleted = true;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Invalidates this challenge, preventing further use.
    /// </summary>
    public void Invalidate()
    {
        IsInvalid = true;
    }

    /// <summary>
    /// Checks if this challenge has expired based on time.
    /// </summary>
    /// <returns>True if the challenge has expired</returns>
    public bool IsExpired()
    {
        return DateTimeOffset.UtcNow > ExpiresAt;
    }

    /// <summary>
    /// Checks if this challenge is still valid for verification attempts.
    /// </summary>
    /// <returns>True if the challenge can still be used</returns>
    public bool IsValid()
    {
        return !IsExpired() && !IsInvalid && !IsCompleted && AttemptCount < MaxAttempts;
    }

    /// <summary>
    /// Gets the number of remaining verification attempts.
    /// </summary>
    /// <returns>Number of attempts remaining</returns>
    public int GetRemainingAttempts()
    {
        if (IsInvalid || IsCompleted)
            return 0;

        return Math.Max(0, MaxAttempts - AttemptCount);
    }

    /// <summary>
    /// Gets the time remaining until this challenge expires.
    /// </summary>
    /// <returns>Time remaining, or zero if expired</returns>
    public TimeSpan GetRemainingTime()
    {
        var remaining = ExpiresAt - DateTimeOffset.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// Updates the metadata for this challenge.
    /// Used to store type-specific information like phone numbers or email addresses.
    /// </summary>
    /// <param name="metadata">JSON string containing metadata</param>
    public void SetMetadata(string metadata)
    {
        if (IsCompleted || IsInvalid)
            throw new InvalidOperationException("Cannot update metadata on completed or invalid challenge");

        Metadata = metadata;
    }

    /// <summary>
    /// Generates a cryptographically secure token for the challenge.
    /// </summary>
    /// <returns>A URL-safe token string</returns>
    private static string GenerateSecureToken()
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[TokenLength];
            rng.GetBytes(bytes);

            // Convert to URL-safe base64
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }
}
