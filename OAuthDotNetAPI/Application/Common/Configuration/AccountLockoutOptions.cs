using System.ComponentModel.DataAnnotations;

namespace Application.Common.Configuration;

/// <summary>
/// Configuration options for account lockout functionality.
/// Controls behavior related to failed login attempts, lockout duration, and security policies.
/// </summary>
public class AccountLockoutOptions
{
    /// <summary>
    /// Gets the configuration section name for account lockout options.
    /// </summary>
    public const string SectionName = "AccountLockout";

    /// <summary>
    /// Gets or sets the number of failed attempts before an account is locked out.
    /// </summary>
    [Range(1, 50, ErrorMessage = "Failed attempt threshold must be between 1 and 50")]
    public int FailedAttemptThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the base lockout duration in minutes for first lockout.
    /// </summary>
    [Range(1, 1440, ErrorMessage = "Base lockout duration must be between 1 and 1440 minutes (24 hours)")]
    public int BaseLockoutDurationMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum lockout duration in minutes to prevent indefinite lockouts.
    /// </summary>
    [Range(5, 10080, ErrorMessage = "Max lockout duration must be between 5 and 10080 minutes (1 week)")]
    public int MaxLockoutDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the time window in minutes after which failed attempt count resets.
    /// </summary>
    [Range(1, 1440, ErrorMessage = "Attempt reset window must be between 1 and 1440 minutes")]
    public int AttemptResetWindowMinutes { get; set; } = 15;

    /// <summary>
    /// Gets or sets whether account lockout functionality is enabled.
    /// </summary>
    public bool EnableAccountLockout { get; set; } = true;

    /// <summary>
    /// Gets or sets whether successful and failed login attempts should be tracked.
    /// </summary>
    public bool TrackLoginAttempts { get; set; } = true;

    /// <summary>
    /// Gets the base lockout duration as a TimeSpan.
    /// </summary>
    public TimeSpan BaseLockoutDuration => TimeSpan.FromMinutes(BaseLockoutDurationMinutes);

    /// <summary>
    /// Gets the maximum lockout duration as a TimeSpan.
    /// </summary>
    public TimeSpan MaxLockoutDuration => TimeSpan.FromMinutes(MaxLockoutDurationMinutes);

    /// <summary>
    /// Gets the attempt reset window as a TimeSpan.
    /// </summary>
    public TimeSpan AttemptResetWindow => TimeSpan.FromMinutes(AttemptResetWindowMinutes);
}
