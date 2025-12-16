using System.ComponentModel.DataAnnotations;

namespace Application.Common.Configuration;

/// <summary>
/// Configuration options for push notification multi-factor authentication.
/// </summary>
public class PushMfaOptions
{
    public const string SectionName = "PushMfaSettings";

    /// <summary>
    /// Gets or sets the expiration time for push challenges in minutes.
    /// </summary>
    [Range(1, 30, ErrorMessage = "Challenge expiry must be between 1 and 30 minutes")]
    public int ChallengeExpiryMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum number of challenges allowed per rate limit window.
    /// </summary>
    [Range(1, 20, ErrorMessage = "Max challenges per window must be between 1 and 20")]
    public int MaxChallengesPerWindow { get; set; } = 5;

    /// <summary>
    /// Gets or sets the rate limit window duration in minutes.
    /// </summary>
    [Range(1, 60, ErrorMessage = "Rate limit window must be between 1 and 60 minutes")]
    public int RateLimitWindowMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the age in hours before expired challenges are cleaned up.
    /// </summary>
    [Range(1, 168, ErrorMessage = "Cleanup age must be between 1 and 168 hours (1 week)")]
    public int CleanupAgeHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets the push notification provider type.
    /// </summary>
    [Required(ErrorMessage = "Provider must be specified")]
    public string Provider { get; set; } = "Mock";
}
