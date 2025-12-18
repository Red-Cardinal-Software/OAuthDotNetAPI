using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Mfa;

/// <summary>
/// DTO containing the information needed for a user to set up TOTP MFA.
/// Includes QR code data and manual entry information.
/// </summary>
public class MfaSetupDto
{
    /// <summary>
    /// The secret key for TOTP generation (Base32 encoded).
    /// This should be displayed to the user for manual entry if QR code scanning fails.
    /// </summary>
    public string Secret { get; init; } = string.Empty;

    /// <summary>
    /// The formatted secret key with spaces for easier manual entry.
    /// Format: ABCD EFGH IJKL MNOP
    /// </summary>
    public string FormattedSecret { get; init; } = string.Empty;

    /// <summary>
    /// The complete URI for generating QR codes.
    /// Format: otpauth://totp/AppName:username?secret=ABC123&issuer=AppName
    /// </summary>
    public string QrCodeUri { get; init; } = string.Empty;

    /// <summary>
    /// Base64 encoded PNG image of the QR code for immediate display.
    /// </summary>
    public string? QrCodeImage { get; init; }

    /// <summary>
    /// The app/service name that will appear in the authenticator app.
    /// </summary>
    public string IssuerName { get; init; } = string.Empty;

    /// <summary>
    /// The account identifier (usually username or email).
    /// </summary>
    public string AccountName { get; init; } = string.Empty;

    /// <summary>
    /// Instructions for the user on how to complete setup.
    /// </summary>
    public string Instructions { get; init; } = string.Empty;
}

/// <summary>
/// DTO for verifying MFA setup during enrollment.
/// </summary>
public class VerifyMfaSetupDto
{
    /// <summary>
    /// The 6-digit TOTP code from the user's authenticator app.
    /// </summary>
    [Required(ErrorMessage = "Code is required")]
    [StringLength(8, MinimumLength = 6, ErrorMessage = "Code must be between 6 and 8 characters")]
    [RegularExpression(@"^\d{6,8}$", ErrorMessage = "Code must be 6-8 digits")]
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Optional friendly name for this MFA method.
    /// </summary>
    [StringLength(100, ErrorMessage = "Name must not exceed 100 characters")]
    public string? Name { get; init; }
}

/// <summary>
/// DTO returned after successful MFA setup verification.
/// Contains recovery codes and important security information.
/// </summary>
public class MfaSetupCompleteDto
{
    /// <summary>
    /// The unique ID of the created MFA method.
    /// </summary>
    public Guid MfaMethodId { get; init; }

    /// <summary>
    /// Recovery codes for emergency access.
    /// Each code can only be used once.
    /// </summary>
    public string[] RecoveryCodes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether this is the user's first MFA method (automatically set as default).
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Security message to display to the user about recovery codes.
    /// </summary>
    public string SecurityMessage { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the MFA method was verified and enabled.
    /// </summary>
    public DateTimeOffset VerifiedAt { get; init; }
}