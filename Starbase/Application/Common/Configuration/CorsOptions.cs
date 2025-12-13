namespace Application.Common.Configuration;

/// <summary>
/// Configuration options for Cross-Origin Resource Sharing (CORS).
/// Provides strongly-typed configuration for CORS policies that can be customized
/// per environment via appsettings.json.
/// </summary>
public class CorsOptions
{
    /// <summary>
    /// Gets the configuration section name for CORS options.
    /// </summary>
    public const string SectionName = "Cors";

    /// <summary>
    /// Gets or sets whether CORS is enabled.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the allowed origins for CORS requests.
    /// Use "*" to allow any origin (not recommended for production with credentials).
    /// Example: ["https://example.com", "https://app.example.com"]
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>
    /// Gets or sets the allowed HTTP methods for CORS requests.
    /// Default: GET, POST, PUT, DELETE, PATCH, OPTIONS
    /// </summary>
    public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"];

    /// <summary>
    /// Gets or sets the allowed headers for CORS requests.
    /// Default: Common headers including Authorization and Content-Type.
    /// </summary>
    public string[] AllowedHeaders { get; set; } = ["Authorization", "Content-Type", "Accept", "X-Requested-With"];

    /// <summary>
    /// Gets or sets the headers exposed to the client in the response.
    /// These headers can be accessed by JavaScript in the browser.
    /// </summary>
    public string[] ExposedHeaders { get; set; } = ["X-Pagination", "X-Total-Count"];

    /// <summary>
    /// Gets or sets whether credentials (cookies, authorization headers) are allowed.
    /// Cannot be used with AllowedOrigins = "*".
    /// Default: true
    /// </summary>
    public bool AllowCredentials { get; set; } = true;

    /// <summary>
    /// Gets or sets the max age in seconds for preflight request caching.
    /// Browsers will cache the preflight response for this duration.
    /// Default: 600 (10 minutes)
    /// </summary>
    public int PreflightMaxAgeSeconds { get; set; } = 600;
}