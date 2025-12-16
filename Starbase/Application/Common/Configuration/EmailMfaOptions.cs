using System.ComponentModel.DataAnnotations;

namespace Application.Common.Configuration;

/// <summary>
/// Configuration options for email-based multi-factor authentication.
/// </summary>
public class EmailMfaOptions
{
    public const string SectionName = "EmailMfaSettings";

    /// <summary>
    /// Gets or sets the maximum number of codes allowed per rate limit window.
    /// </summary>
    [Range(1, 10, ErrorMessage = "Max codes per window must be between 1 and 10")]
    public int MaxCodesPerWindow { get; set; } = 3;

    /// <summary>
    /// Gets or sets the rate limit window duration in minutes.
    /// </summary>
    [Range(5, 120, ErrorMessage = "Rate limit window must be between 5 and 120 minutes")]
    public int RateLimitWindowMinutes { get; set; } = 15;

    /// <summary>
    /// Gets or sets the expiration time for email codes in minutes.
    /// </summary>
    [Range(1, 30, ErrorMessage = "Code expiry must be between 1 and 30 minutes")]
    public int CodeExpiryMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the age in hours before expired codes are cleaned up.
    /// </summary>
    [Range(1, 168, ErrorMessage = "Cleanup age must be between 1 and 168 hours (1 week)")]
    public int CleanupAgeHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets the application name for email templates.
    /// </summary>
    [Required(ErrorMessage = "App name is required for email templates")]
    [StringLength(100, ErrorMessage = "App name cannot exceed 100 characters")]
    public string AppName { get; set; } = "Starbase Template .NET API";

    /// <summary>
    /// Gets or sets whether to enable security warnings in emails.
    /// </summary>
    public bool EnableSecurityWarnings { get; set; } = true;
}
