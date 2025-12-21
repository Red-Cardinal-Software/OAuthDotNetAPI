using Domain.Attributes;
using Domain.Entities.Identity;
using Domain.Exceptions;

namespace Domain.Entities.Security;

/// <summary>
/// Represents a multi-factor authentication method configured for a user.
/// Supports multiple MFA types (TOTP, WebAuthn, SMS, Email) with a flexible schema
/// that can accommodate different authentication mechanisms without database changes.
/// </summary>
[Audited]
public class MfaMethod
{
    /// <summary>
    /// Unique identifier for the MFA method.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The user who owns this MFA method.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The type of MFA method (TOTP, WebAuthn, SMS, Email).
    /// </summary>
    public MfaType Type { get; private set; }

    /// <summary>
    /// Encrypted secret data. Contents vary by MFA type:
    /// - TOTP: Base32 encoded secret
    /// - WebAuthn: Credential ID
    /// - SMS/Email: null (no secret needed)
    /// </summary>
    public string? Secret { get; private set; }

    /// <summary>
    /// JSON metadata specific to the MFA type.
    /// Stores additional configuration and state information.
    /// </summary>
    public string? Metadata { get; private set; }

    /// <summary>
    /// User-friendly name for this method (e.g., "Work Phone", "Personal Authenticator").
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// Whether this MFA method is currently active and can be used for authentication.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Whether this is the user's primary/default MFA method.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// Timestamp when this MFA method was registered.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Timestamp when this MFA method was verified/activated.
    /// </summary>
    public DateTimeOffset? VerifiedAt { get; private set; }

    /// <summary>
    /// Timestamp when this MFA method was last used successfully.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; private set; }

    /// <summary>
    /// Timestamp when this MFA method was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Navigation property for recovery codes associated with this method.
    /// </summary>
    public ICollection<MfaRecoveryCode> RecoveryCodes { get; private set; }

    /// <summary>
    /// Navigation property to the user who owns this MFA method.
    /// </summary>
    public AppUser User { get; private set; } = null!;

    /// <summary>
    /// Private constructor for Entity Framework Core.
    /// </summary>
    private MfaMethod()
    {
        RecoveryCodes = new List<MfaRecoveryCode>();
    }

    /// <summary>
    /// Creates a new TOTP (Time-based One-Time Password) MFA method.
    /// </summary>
    /// <param name="userId">The user ID this method belongs to</param>
    /// <param name="secret">The Base32 encoded secret</param>
    /// <param name="name">Optional friendly name</param>
    /// <returns>A new unverified TOTP method</returns>
    public static MfaMethod CreateTotp(Guid userId, string secret, string? name = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Secret cannot be empty", nameof(secret));

        var now = DateTimeOffset.UtcNow;

        return new MfaMethod
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = MfaType.Totp,
            Secret = secret, // Should be encrypted before storage
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                Algorithm = "SHA1",
                Digits = 6,
                Period = 30
            }),
            Name = name ?? "Authenticator App",
            IsEnabled = false, // Must be verified first
            IsDefault = false,
            CreatedAt = now,
            UpdatedAt = now,
            RecoveryCodes = new List<MfaRecoveryCode>()
        };
    }

    /// <summary>
    /// Creates a new WebAuthn/FIDO2 MFA method.
    /// </summary>
    public static MfaMethod CreateWebAuthn(Guid userId, string credentialId, string publicKey, string deviceName)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        var now = DateTimeOffset.UtcNow;

        return new MfaMethod
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = MfaType.WebAuthn,
            Secret = credentialId,
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                PublicKey = publicKey,
                Counter = 0,
                DeviceName = deviceName,
                AAGUID = string.Empty
            }),
            Name = deviceName,
            IsEnabled = true, // WebAuthn is verified during registration
            IsDefault = false,
            CreatedAt = now,
            VerifiedAt = now,
            UpdatedAt = now,
            RecoveryCodes = new List<MfaRecoveryCode>()
        };
    }

    /// <summary>
    /// Creates a new Push notification MFA method.
    /// </summary>
    public static MfaMethod CreatePush(Guid userId, string name)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        var now = DateTimeOffset.UtcNow;

        return new MfaMethod
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = MfaType.Push,
            Secret = null, // Push doesn't need a stored secret
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                Devices = new List<object>() // Will store device IDs
            }),
            Name = name ?? "Push Notifications",
            IsEnabled = false, // Must have at least one device
            IsDefault = false,
            CreatedAt = now,
            UpdatedAt = now,
            RecoveryCodes = new List<MfaRecoveryCode>()
        };
    }

    /// <summary>
    /// Creates a new Email-based MFA method.
    /// </summary>
    public static MfaMethod CreateEmail(Guid userId, string email)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        var now = DateTimeOffset.UtcNow;

        return new MfaMethod
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = MfaType.Email,
            Secret = null, // Email doesn't need a stored secret
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                EmailAddress = email,
                Verified = false
            }),
            Name = $"Email ({email})",
            IsEnabled = false, // Must be verified first
            IsDefault = false,
            CreatedAt = now,
            UpdatedAt = now,
            RecoveryCodes = new List<MfaRecoveryCode>()
        };
    }

    /// <summary>
    /// Verifies the MFA method and enables it for use.
    /// Generates recovery codes for backup access.
    /// </summary>
    public void Verify()
    {
        if (IsEnabled)
            throw new InvalidOperationException("MFA method is already verified");

        IsEnabled = true;
        VerifiedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        // Recovery codes will be generated by the MfaRecoveryCodeService after entity creation
    }

    /// <summary>
    /// Marks this MFA method as the user's default.
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Removes the default status from this MFA method.
    /// </summary>
    public void RemoveDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Records successful use of this MFA method.
    /// </summary>
    public void RecordUsage()
    {
        LastUsedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Disables this MFA method.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        IsDefault = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the friendly name of this MFA method.
    /// </summary>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Sets recovery codes for this MFA method.
    /// This method is used by the service layer to add securely hashed recovery codes.
    /// </summary>
    /// <param name="newRecoveryCodes">The recovery codes to add</param>
    public void SetRecoveryCodes(IEnumerable<MfaRecoveryCode> newRecoveryCodes)
    {
        if (newRecoveryCodes == null)
            throw new ArgumentNullException(nameof(newRecoveryCodes));

        // Clear existing unused codes
        var usedCodes = RecoveryCodes.Where(c => c.IsUsed).ToList();
        RecoveryCodes.Clear();

        // Re-add used codes for audit trail
        foreach (var usedCode in usedCodes)
        {
            RecoveryCodes.Add(usedCode);
        }

        // Add new codes
        foreach (var code in newRecoveryCodes)
        {
            RecoveryCodes.Add(code);
        }
    }

    /// <summary>
    /// Gets the plain text codes from newly generated recovery codes.
    /// This should only be called immediately after setting new recovery codes.
    /// </summary>
    /// <returns>The newly generated recovery codes in plain text</returns>
    public IReadOnlyList<string> GetNewRecoveryCodes()
    {
        UpdatedAt = DateTimeOffset.UtcNow;

        return RecoveryCodes
            .Where(c => !c.IsUsed && !string.IsNullOrEmpty(c.Code))
            .Select(c => c.Code)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Finds an unused recovery code by ID and marks it as used if found.
    /// This method is used by the service layer after external validation.
    /// </summary>
    /// <param name="recoveryCodeId">The ID of the recovery code to use</param>
    /// <returns>True if the recovery code was found and marked as used</returns>
    public bool TryUseRecoveryCode(Guid recoveryCodeId)
    {
        var recoveryCode = RecoveryCodes
            .FirstOrDefault(c => c.Id == recoveryCodeId && !c.IsUsed);

        if (recoveryCode?.TryMarkAsUsed() == true)
        {
            RecordUsage();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all unused recovery codes for this MFA method.
    /// Used by the service layer for validation.
    /// </summary>
    /// <returns>Collection of unused recovery codes</returns>
    public IReadOnlyList<MfaRecoveryCode> GetUnusedRecoveryCodes()
    {
        return RecoveryCodes
            .Where(c => !c.IsUsed)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets the count of unused recovery codes.
    /// </summary>
    public int GetUnusedRecoveryCodeCount()
    {
        return RecoveryCodes.Count(c => !c.IsUsed);
    }

    /// <summary>
    /// Stores a setup verification code in the metadata for email MFA.
    /// </summary>
    /// <param name="hashedCode">The hashed verification code</param>
    /// <param name="expiresAt">When the code expires</param>
    public void StoreSetupVerificationCode(string hashedCode, DateTimeOffset expiresAt)
    {
        if (Type != MfaType.Email)
            throw new InvalidOperationException("Setup verification codes are only for email MFA");

        if (IsEnabled)
            throw new InvalidOperationException("Cannot store setup code for already verified method");

        // Parse existing metadata
        var metadataObj = string.IsNullOrWhiteSpace(Metadata)
            ? new Dictionary<string, object>()
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata)!;

        // Add verification code info
        metadataObj["SetupVerificationCode"] = hashedCode;
        metadataObj["SetupCodeExpiresAt"] = expiresAt.ToString("O");
        metadataObj["SetupCodeCreatedAt"] = DateTimeOffset.UtcNow.ToString("O");

        // Serialize back to JSON
        Metadata = System.Text.Json.JsonSerializer.Serialize(metadataObj);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the setup verification code from metadata if it exists and hasn't expired.
    /// </summary>
    /// <returns>The hashed code if valid, null otherwise</returns>
    public string? GetSetupVerificationCode()
    {
        if (Type != MfaType.Email || IsEnabled || string.IsNullOrWhiteSpace(Metadata))
            return null;

        try
        {
            var metadataObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata);
            if (metadataObj == null)
                return null;

            // Check if code exists
            if (!metadataObj.TryGetValue("SetupVerificationCode", out var codeObj) ||
                !metadataObj.TryGetValue("SetupCodeExpiresAt", out var expiresObj))
                return null;

            // Check if expired
            var expiresAt = DateTimeOffset.Parse(expiresObj.ToString()!);
            if (DateTimeOffset.UtcNow > expiresAt)
                return null;

            return codeObj.ToString();
        }
        catch
        {
            // Invalid metadata
            return null;
        }
    }

    /// <summary>
    /// Clears the setup verification code from metadata after successful verification.
    /// </summary>
    public void ClearSetupVerificationCode()
    {
        if (string.IsNullOrWhiteSpace(Metadata))
            return;

        try
        {
            var metadataObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata);
            if (metadataObj != null)
            {
                metadataObj.Remove("SetupVerificationCode");
                metadataObj.Remove("SetupCodeExpiresAt");
                metadataObj.Remove("SetupCodeCreatedAt");
                Metadata = System.Text.Json.JsonSerializer.Serialize(metadataObj);
                UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
        catch
        {
            // Ignore metadata parsing errors
        }
    }
}

/// <summary>
/// Defines the types of multi-factor authentication methods supported.
/// </summary>
public enum MfaType
{
    /// <summary>
    /// Time-based One-Time Password (Google Authenticator, Authy, etc.)
    /// </summary>
    Totp = 1,

    /// <summary>
    /// WebAuthn/FIDO2 (Passkeys, Security Keys)
    /// </summary>
    WebAuthn = 2,

    /// <summary>
    /// Email verification code
    /// </summary>
    Email = 3,

    /// <summary>
    /// Push notification to mobile app
    /// </summary>
    Push = 4
}
