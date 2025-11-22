namespace Application.Common.Constants;

/// <summary>
/// Represents a static class that contains system default values used across the application.
/// These defaults are utilized as fallback values when  specific configurations
/// are not provided in the application settings.
/// </summary>
public static class SystemDefaults
{
    public const int DefaultPasswordResetExpirationInHours = 1;
    public const int DefaultPasswordMinimumLength = 8;
    public const int DefaultPasswordMaximumLength = 64;
    public const int DefaultJwtExpirationTimeInMinutes = 5;
    public const int DefaultRefreshTokenExpirationTimeInHours = 24;

    // Rate Limiting Defaults
    public const int DefaultRateLimitAuthPermitLimit = 5;
    public const int DefaultRateLimitAuthWindowMinutes = 1;
    public const int DefaultRateLimitPasswordResetPermitLimit = 3;
    public const int DefaultRateLimitPasswordResetWindowMinutes = 5;
    public const int DefaultRateLimitApiPermitLimit = 100;
    public const int DefaultRateLimitApiWindowMinutes = 1;
    public const int DefaultRateLimitGlobalPermitLimit = 200;
    public const int DefaultRateLimitGlobalWindowMinutes = 1;
}
