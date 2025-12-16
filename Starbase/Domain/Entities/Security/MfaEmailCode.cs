using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Domain.Entities.Security;

/// <summary>
/// Represents a one-time email verification code for MFA.
/// Stores codes securely with expiration and usage tracking.
/// </summary>
public class MfaEmailCode
{
    /// <summary>
    /// Unique identifier for the email code.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The MFA challenge this code belongs to.
    /// </summary>
    public Guid MfaChallengeId { get; private set; }

    /// <summary>
    /// Navigation property to the parent MFA challenge.
    /// </summary>
    public MfaChallenge Challenge { get; private set; } = null!;

    /// <summary>
    /// The user this code was sent to.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Email address where the code was sent.
    /// </summary>
    public string EmailAddress { get; private set; }

    /// <summary>
    /// Hashed version of the verification code.
    /// </summary>
    public string HashedCode { get; private set; }

    /// <summary>
    /// Whether this code has been used.
    /// </summary>
    public bool IsUsed { get; private set; }

    /// <summary>
    /// When this code expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// When the code was sent.
    /// </summary>
    public DateTimeOffset SentAt { get; private set; }

    /// <summary>
    /// When the code was used (if applicable).
    /// </summary>
    public DateTimeOffset? UsedAt { get; private set; }

    /// <summary>
    /// Number of verification attempts for this code.
    /// </summary>
    public int AttemptCount { get; private set; }

    /// <summary>
    /// IP address where the code was requested from.
    /// </summary>
    public string? IpAddress { get; private set; }

    // Configuration
    private const int CodeLength = 8;
    private const int MaxAttempts = 3;
    private const int ValidityMinutes = 5;

    /// <summary>
    /// Private constructor for Entity Framework Core.
    /// </summary>
    private MfaEmailCode()
    {
        EmailAddress = string.Empty;
        HashedCode = string.Empty;
    }

    /// <summary>
    /// Creates a new email verification code for MFA.
    /// </summary>
    /// <param name="challengeId">The MFA challenge ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="emailAddress">The email address to send to</param>
    /// <param name="hashedCode">The hashed verification code</param>
    /// <param name="ipAddress">Optional IP address for security tracking</param>
    /// <returns>Tuple of (EmailCode entity, Plain text code to send)</returns>
    public static (MfaEmailCode EmailCode, string PlainCode) Create(
        Guid challengeId,
        Guid userId,
        string emailAddress,
        string hashedCode,
        string? ipAddress = null)
    {
        if (challengeId == Guid.Empty)
            throw new ArgumentException("Challenge ID cannot be empty", nameof(challengeId));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(emailAddress))
            throw new ArgumentException("Email address cannot be empty", nameof(emailAddress));

        // Generate secure numeric code
        var plainCode = GenerateSecureCode();

        var emailCode = new MfaEmailCode
        {
            Id = Guid.NewGuid(),
            MfaChallengeId = challengeId,
            UserId = userId,
            EmailAddress = emailAddress.ToLowerInvariant(),
            HashedCode = hashedCode,
            IsUsed = false,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(ValidityMinutes),
            SentAt = DateTimeOffset.UtcNow,
            AttemptCount = 0,
            IpAddress = ipAddress
        };

        return (emailCode, plainCode);
    }

    /// <summary>
    /// Records an attempt to verify this code.
    /// </summary>
    /// <returns>True if attempts are still allowed</returns>
    public bool RecordAttempt()
    {
        if (IsUsed || DateTimeOffset.UtcNow > ExpiresAt || AttemptCount >= MaxAttempts)
            return false;

        AttemptCount++;
        return true;
    }

    /// <summary>
    /// Marks this code as successfully used.
    /// </summary>
    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Checks if this code is still valid (not used, not expired, attempts remaining).
    /// </summary>
    public bool IsValid()
    {
        return !IsUsed
            && DateTimeOffset.UtcNow <= ExpiresAt
            && AttemptCount < MaxAttempts;
    }

    /// <summary>
    /// Gets the number of remaining attempts.
    /// </summary>
    public int GetRemainingAttempts()
    {
        return Math.Max(0, MaxAttempts - AttemptCount);
    }

    /// <summary>
    /// Marks this code as invalid/expired.
    /// </summary>
    public void Invalidate()
    {
        IsUsed = true;
        UsedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Generates a cryptographically secure numeric code.
    /// </summary>
    private static string GenerateSecureCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);

        // Convert to uint and take modulo to get 8-digit number
        var value = BinaryPrimitives.ReadUInt32BigEndian(bytes);
        var code = (value % 90000000) + 10000000; // Ensures 8 digits

        return code.ToString();
    }
}
