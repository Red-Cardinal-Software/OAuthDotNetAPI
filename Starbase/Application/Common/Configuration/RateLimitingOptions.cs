using System.ComponentModel.DataAnnotations;

namespace Application.Common.Configuration;

/// <summary>
/// Configuration options for rate limiting policies.
/// Provides strongly-typed configuration for different API endpoint categories
/// with validation to prevent misconfiguration that could impact security or performance.
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Gets the configuration section name for rate limiting options.
    /// </summary>
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Gets or sets the rate limiting configuration for authentication endpoints.
    /// These are the most restrictive limits to prevent brute force attacks.
    /// </summary>
    public AuthRateLimitOptions Auth { get; set; } = new();

    /// <summary>
    /// Gets or sets the rate limiting configuration for password reset endpoints.
    /// Moderate restrictions to prevent email spam while allowing legitimate usage.
    /// </summary>
    public PasswordResetRateLimitOptions PasswordReset { get; set; } = new();

    /// <summary>
    /// Gets or sets the rate limiting configuration for general API endpoints.
    /// More permissive limits for normal API operations.
    /// </summary>
    public ApiRateLimitOptions Api { get; set; } = new();

    /// <summary>
    /// Gets or sets the rate limiting configuration for health check endpoints.
    /// Moderate limits to prevent abuse of monitoring endpoints.
    /// </summary>
    public HealthRateLimitOptions Health { get; set; } = new();

    /// <summary>
    /// Gets or sets the rate limiting configuration for MFA setup endpoints.
    /// Restrictive limits to prevent brute force attacks during MFA enrollment.
    /// </summary>
    public MfaSetupRateLimitOptions MfaSetup { get; set; } = new();

    /// <summary>
    /// Gets or sets the global rate limiting configuration.
    /// Prevents any single IP from overwhelming the entire API.
    /// </summary>
    public GlobalRateLimitOptions Global { get; set; } = new();
}

/// <summary>
/// Rate limiting options for authentication endpoints (login, refresh token).
/// Most restrictive to prevent brute force attacks.
/// </summary>
public class AuthRateLimitOptions
{
    /// <summary>
    /// Gets or sets the maximum number of requests allowed within the time window.
    /// </summary>
    [Range(1, 50, ErrorMessage = "Auth permit limit must be between 1 and 50")]
    public int PermitLimit { get; set; } = 5;

    /// <summary>
    /// Gets or sets the time window in minutes for the permit limit.
    /// </summary>
    [Range(1, 60, ErrorMessage = "Auth window must be between 1 and 60 minutes")]
    public int WindowMinutes { get; set; } = 1;
}

/// <summary>
/// Rate limiting options for password reset endpoints.
/// Moderate restrictions to prevent email spam.
/// </summary>
public class PasswordResetRateLimitOptions
{
    /// <summary>
    /// Gets or sets the maximum number of requests allowed within the time window.
    /// </summary>
    [Range(1, 20, ErrorMessage = "PasswordReset permit limit must be between 1 and 20")]
    public int PermitLimit { get; set; } = 3;

    /// <summary>
    /// Gets or sets the time window in minutes for the permit limit.
    /// </summary>
    [Range(1, 60, ErrorMessage = "PasswordReset window must be between 1 and 60 minutes")]
    public int WindowMinutes { get; set; } = 5;
}

/// <summary>
/// Rate limiting options for general API endpoints.
/// More permissive for normal operations.
/// </summary>
public class ApiRateLimitOptions
{
    /// <summary>
    /// Gets or sets the maximum number of requests allowed within the time window.
    /// </summary>
    [Range(10, 1000, ErrorMessage = "Api permit limit must be between 10 and 1000")]
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the time window in minutes for the permit limit.
    /// </summary>
    [Range(1, 60, ErrorMessage = "Api window must be between 1 and 60 minutes")]
    public int WindowMinutes { get; set; } = 1;
}

/// <summary>
/// Rate limiting options for health check endpoints.
/// Moderate limits to prevent monitoring endpoint abuse.
/// </summary>
public class HealthRateLimitOptions
{
    /// <summary>
    /// Gets or sets the maximum number of requests allowed within the time window.
    /// </summary>
    [Range(5, 200, ErrorMessage = "Health permit limit must be between 5 and 200")]
    public int PermitLimit { get; set; } = 30;

    /// <summary>
    /// Gets or sets the time window in minutes for the permit limit.
    /// </summary>
    [Range(1, 60, ErrorMessage = "Health window must be between 1 and 60 minutes")]
    public int WindowMinutes { get; set; } = 1;
}

/// <summary>
/// Rate limiting options for MFA setup and verification endpoints.
/// Restrictive limits to prevent brute force attacks during MFA enrollment.
/// </summary>
public class MfaSetupRateLimitOptions
{
    /// <summary>
    /// Gets or sets the maximum number of requests allowed within the time window.
    /// </summary>
    [Range(3, 20, ErrorMessage = "MfaSetup permit limit must be between 3 and 20")]
    public int PermitLimit { get; set; } = 10;

    /// <summary>
    /// Gets or sets the time window in minutes for the permit limit.
    /// </summary>
    [Range(1, 60, ErrorMessage = "MfaSetup window must be between 1 and 60 minutes")]
    public int WindowMinutes { get; set; } = 5;
}

/// <summary>
/// Global rate limiting options applied per IP address.
/// Prevents any single IP from overwhelming the API.
/// </summary>
public class GlobalRateLimitOptions
{
    /// <summary>
    /// Gets or sets the maximum number of requests allowed within the time window per IP.
    /// </summary>
    [Range(50, 5000, ErrorMessage = "Global permit limit must be between 50 and 5000")]
    public int PermitLimit { get; set; } = 200;

    /// <summary>
    /// Gets or sets the time window in minutes for the permit limit.
    /// </summary>
    [Range(1, 60, ErrorMessage = "Global window must be between 1 and 60 minutes")]
    public int WindowMinutes { get; set; } = 1;
}
