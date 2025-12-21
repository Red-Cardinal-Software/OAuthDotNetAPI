using System.ComponentModel.DataAnnotations;
using Domain.Entities.Security;

namespace Application.DTOs.Mfa;

/// <summary>
/// DTO representing a user's MFA method for display and management.
/// </summary>
public class MfaMethodDto
{
    /// <summary>
    /// Unique identifier for this MFA method.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The type of MFA method (TOTP, WebAuthn, SMS, Email).
    /// </summary>
    public MfaType Type { get; init; }

    /// <summary>
    /// User-friendly name for this method.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Whether this method is currently enabled and can be used.
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Whether this is the user's default MFA method.
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// When this method was set up.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When this method was last used for authentication.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; init; }

    /// <summary>
    /// Number of unused recovery codes available.
    /// </summary>
    public int UnusedRecoveryCodeCount { get; init; }

    /// <summary>
    /// Additional metadata about the method (e.g., phone number for SMS).
    /// </summary>
    public string? DisplayInfo { get; init; }
}

/// <summary>
/// DTO for updating an MFA method's settings.
/// </summary>
public class UpdateMfaMethodDto
{
    /// <summary>
    /// New friendly name for the method.
    /// </summary>
    [StringLength(100, ErrorMessage = "Name must not exceed 100 characters")]
    public string? Name { get; init; }

    /// <summary>
    /// Whether to enable or disable this method.
    /// </summary>
    public bool? IsEnabled { get; init; }

    /// <summary>
    /// Whether to set this as the default method.
    /// </summary>
    public bool? IsDefault { get; init; }
}

/// <summary>
/// DTO for MFA method statistics and overview.
/// </summary>
public class MfaOverviewDto
{
    /// <summary>
    /// Whether the user has any enabled MFA methods.
    /// </summary>
    public bool HasEnabledMfa { get; init; }

    /// <summary>
    /// Total number of configured MFA methods.
    /// </summary>
    public int TotalMethods { get; init; }

    /// <summary>
    /// Number of enabled MFA methods.
    /// </summary>
    public int EnabledMethods { get; init; }

    /// <summary>
    /// The user's MFA methods.
    /// </summary>
    public MfaMethodDto[] Methods { get; init; } = Array.Empty<MfaMethodDto>();

    /// <summary>
    /// Available MFA types that the user can set up.
    /// </summary>
    public string[] AvailableTypes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether the user should be encouraged to set up MFA.
    /// </summary>
    public bool ShouldPromptSetup { get; init; }
}