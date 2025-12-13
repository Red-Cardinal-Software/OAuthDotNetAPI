using System.ComponentModel.DataAnnotations;

namespace Application.Common.Configuration;

/// <summary>
/// Configuration options for general multi-factor authentication settings.
/// </summary>
public class MfaOptions
{
    public const string SectionName = "MfaSettings";

    /// <summary>
    /// Gets or sets the maximum number of active challenges per user.
    /// </summary>
    [Range(1, 10, ErrorMessage = "Max active challenges must be between 1 and 10")]
    public int MaxActiveChallenges { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum number of challenges per rate limit window.
    /// </summary>
    [Range(1, 20, ErrorMessage = "Max challenges per window must be between 1 and 20")]
    public int MaxChallengesPerWindow { get; set; } = 5;

    /// <summary>
    /// Gets or sets the rate limit window duration in minutes.
    /// </summary>
    [Range(1, 60, ErrorMessage = "Rate limit window must be between 1 and 60 minutes")]
    public int RateLimitWindowMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the expiration time for MFA challenges in minutes.
    /// </summary>
    [Range(1, 30, ErrorMessage = "Challenge expiry must be between 1 and 30 minutes")]
    public int ChallengeExpiryMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether to prompt users to set up MFA after login.
    /// </summary>
    public bool PromptSetup { get; set; } = true;
}
