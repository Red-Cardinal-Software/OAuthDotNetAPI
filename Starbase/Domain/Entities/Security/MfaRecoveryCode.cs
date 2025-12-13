using System.Security.Cryptography;

namespace Domain.Entities.Security;

/// <summary>
/// Represents a one-time use recovery code for multi-factor authentication.
/// Recovery codes provide emergency access when the primary MFA method is unavailable.
/// </summary>
public class MfaRecoveryCode
{
    /// <summary>
    /// Unique identifier for the recovery code.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The MFA method this recovery code belongs to.
    /// </summary>
    public Guid MfaMethodId { get; private set; }

    /// <summary>
    /// The hashed recovery code. Plain text code is never stored.
    /// </summary>
    public string HashedCode { get; private set; } = string.Empty;

    /// <summary>
    /// Whether this recovery code has been used.
    /// </summary>
    public bool IsUsed { get; private set; }

    /// <summary>
    /// Timestamp when this recovery code was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Timestamp when this recovery code was used (if applicable).
    /// </summary>
    public DateTimeOffset? UsedAt { get; private set; }

    /// <summary>
    /// Navigation property to the parent MFA method.
    /// </summary>
    public MfaMethod MfaMethod { get; private set; } = null!;

    /// <summary>
    /// The plain text code (only available immediately after generation).
    /// This is not persisted to the database.
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Private constructor for Entity Framework Core.
    /// </summary>
    private MfaRecoveryCode()
    {
        HashedCode = string.Empty;
    }

    /// <summary>
    /// Creates a new recovery code for an MFA method with pre-computed hash.
    /// </summary>
    /// <param name="mfaMethodId">The ID of the MFA method</param>
    /// <param name="hashedCode">The pre-computed secure hash of the recovery code</param>
    /// <param name="plainCode">The plain text recovery code (for immediate use only)</param>
    /// <returns>A new recovery code instance</returns>
    public static MfaRecoveryCode Create(Guid mfaMethodId, string hashedCode, string plainCode)
    {
        if (mfaMethodId == Guid.Empty)
            throw new ArgumentException("MFA method ID cannot be empty", nameof(mfaMethodId));

        if (string.IsNullOrWhiteSpace(hashedCode))
            throw new ArgumentException("Hashed code cannot be empty", nameof(hashedCode));

        if (string.IsNullOrWhiteSpace(plainCode))
            throw new ArgumentException("Plain code cannot be empty", nameof(plainCode));

        return new MfaRecoveryCode
        {
            Id = Guid.NewGuid(),
            MfaMethodId = mfaMethodId,
            HashedCode = hashedCode,
            IsUsed = false,
            CreatedAt = DateTimeOffset.UtcNow,
            Code = plainCode // This is only available right after generation
        };
    }

    /// <summary>
    /// Marks this recovery code as used after external validation.
    /// </summary>
    /// <returns>True if successfully marked as used, false if already used</returns>
    public bool TryMarkAsUsed()
    {
        if (IsUsed)
            return false;

        IsUsed = true;
        UsedAt = DateTimeOffset.UtcNow;
        return true;
    }


    /// <summary>
    /// Generates a cryptographically secure recovery code.
    /// </summary>
    /// <returns>A recovery code in format XXXX-XXXX-XXXX-XXXX</returns>
    public static string GenerateSecureCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        const int codeLength = 16;

        var result = new char[codeLength + 3]; // Include space for hyphens
        var position = 0;

        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[codeLength];
            rng.GetBytes(bytes);

            for (int i = 0; i < codeLength; i++)
            {
                // Add hyphen every 4 characters
                if (i > 0 && i % 4 == 0)
                {
                    result[position++] = '-';
                }

                result[position++] = chars[bytes[i] % chars.Length];
            }
        }

        return new string(result);
    }

    /// <summary>
    /// Normalizes a recovery code for consistent processing.
    /// </summary>
    /// <param name="code">The plain text code to normalize</param>
    /// <returns>Normalized code with hyphens removed and uppercase</returns>
    public static string NormalizeCode(string code)
    {
        return code.Replace("-", "").ToUpperInvariant();
    }
}
