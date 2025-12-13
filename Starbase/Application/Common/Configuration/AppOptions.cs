using System.ComponentModel.DataAnnotations;

namespace Application.Common.Configuration;

/// <summary>
/// Configuration options for general application settings.
/// </summary>
public class AppOptions
{
    /// <summary>
    /// Gets or sets the application name.
    /// </summary>
    [Required(ErrorMessage = "Application name is required")]
    [StringLength(100, ErrorMessage = "Application name cannot exceed 100 characters")]
    public string AppName { get; set; } = "Starbase Template .NET API";

    /// <summary>
    /// Gets or sets the JWT signing key.
    /// </summary>
    [Required(ErrorMessage = "JWT signing key is required")]
    [MinLength(32, ErrorMessage = "JWT signing key must be at least 32 characters")]
    public string JwtSigningKey { get; set; } = null!;

    /// <summary>
    /// Gets or sets the JWT issuer (who issued the token).
    /// </summary>
    [Required(ErrorMessage = "JWT issuer is required")]
    public string JwtIssuer { get; set; } = null!;

    /// <summary>
    /// Gets or sets the JWT audience (who the token is intended for).
    /// </summary>
    [Required(ErrorMessage = "JWT audience is required")]
    public string JwtAudience { get; set; } = null!;

    /// <summary>
    /// Gets or sets the JWT expiration time in minutes.
    /// </summary>
    [Range(1, 1440, ErrorMessage = "JWT expiration must be between 1 and 1440 minutes (24 hours)")]
    public int JwtExpirationTimeMinutes { get; set; } = 15;

    /// <summary>
    /// Gets or sets the refresh token expiration time in hours.
    /// </summary>
    [Range(1, 8760, ErrorMessage = "Refresh token expiration must be between 1 and 8760 hours (1 year)")]
    public int RefreshTokenExpirationTimeHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets the password reset expiration time in hours.
    /// </summary>
    [Range(1, 72, ErrorMessage = "Password reset expiration must be between 1 and 72 hours")]
    public int PasswordResetExpirationTimeHours { get; set; } = 1;

    /// <summary>
    /// Gets or sets the minimum password length.
    /// </summary>
    [Range(6, 128, ErrorMessage = "Minimum password length must be between 6 and 128 characters")]
    public int PasswordMinimumLength { get; set; } = 8;

    /// <summary>
    /// Gets or sets the maximum password length.
    /// </summary>
    [Range(8, 512, ErrorMessage = "Maximum password length must be between 8 and 512 characters")]
    public int PasswordMaximumLength { get; set; } = 64;
}
