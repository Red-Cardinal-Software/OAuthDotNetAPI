using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Web.Middleware;

/// <summary>
/// Middleware that adds security headers to all HTTP responses.
/// These headers help protect against common web vulnerabilities.
/// </summary>
public class SecurityHeadersMiddleware(
    RequestDelegate next,
    IHostEnvironment environment,
    SecurityHeadersOptions? options = null)
{
    private readonly SecurityHeadersOptions _options = options ?? new SecurityHeadersOptions();
    private readonly bool _isDevelopment = environment.IsDevelopment();

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before the response is sent
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // X-Frame-Options: Prevents clickjacking by controlling iframe embedding
            // DENY = never allow framing, SAMEORIGIN = only same origin can frame
            if (!headers.ContainsKey("X-Frame-Options"))
            {
                headers["X-Frame-Options"] = _options.FrameOptions;
            }

            // X-Content-Type-Options: Prevents MIME type sniffing
            // Browsers will strictly follow the declared Content-Type
            if (!headers.ContainsKey("X-Content-Type-Options"))
            {
                headers["X-Content-Type-Options"] = "nosniff";
            }

            // X-XSS-Protection: Legacy XSS protection for older browsers
            // Modern browsers use CSP instead, but this helps older clients
            if (!headers.ContainsKey("X-XSS-Protection"))
            {
                headers["X-XSS-Protection"] = "1; mode=block";
            }

            // Referrer-Policy: Controls how much referrer information is sent
            // strict-origin-when-cross-origin = full URL for same-origin, origin only for cross-origin
            if (!headers.ContainsKey("Referrer-Policy"))
            {
                headers["Referrer-Policy"] = _options.ReferrerPolicy;
            }

            // Content-Security-Policy: Controls which resources the browser can load
            // Skipped in development to allow Swagger UI to function
            // Production uses a restrictive API-focused policy
            if (!_isDevelopment && !headers.ContainsKey("Content-Security-Policy") && !string.IsNullOrEmpty(_options.ContentSecurityPolicy))
            {
                headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
            }

            // Permissions-Policy: Controls which browser features can be used
            // Restricts access to sensitive APIs like camera, microphone, geolocation
            if (!headers.ContainsKey("Permissions-Policy") && !string.IsNullOrEmpty(_options.PermissionsPolicy))
            {
                headers["Permissions-Policy"] = _options.PermissionsPolicy;
            }

            // X-Permitted-Cross-Domain-Policies: Controls Adobe Flash/PDF cross-domain access
            // none = disallow all cross-domain policy files
            if (!headers.ContainsKey("X-Permitted-Cross-Domain-Policies"))
            {
                headers["X-Permitted-Cross-Domain-Policies"] = "none";
            }

            // Remove headers that leak server information
            if (_options.RemoveServerHeader)
            {
                headers.Remove("Server");
                headers.Remove("X-Powered-By");
                headers.Remove("X-AspNet-Version");
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}

/// <summary>
/// Configuration options for security headers.
/// Allows customization of header values per environment or application needs.
/// </summary>
public class SecurityHeadersOptions
{
    /// <summary>
    /// X-Frame-Options header value. Default: DENY
    /// Options: DENY, SAMEORIGIN, ALLOW-FROM uri
    /// </summary>
    public string FrameOptions { get; set; } = "DENY";

    /// <summary>
    /// Referrer-Policy header value. Default: strict-origin-when-cross-origin
    /// </summary>
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    /// <summary>
    /// Content-Security-Policy header value.
    /// Default is a restrictive API-focused policy.
    /// Set to null or empty to skip this header.
    /// </summary>
    public string? ContentSecurityPolicy { get; set; } = "default-src 'none'; frame-ancestors 'none'";

    /// <summary>
    /// Permissions-Policy header value.
    /// Controls access to browser features.
    /// Set to null or empty to skip this header.
    /// </summary>
    public string? PermissionsPolicy { get; set; } = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

    /// <summary>
    /// Whether to remove Server, X-Powered-By, and X-AspNet-Version headers.
    /// Default: true
    /// </summary>
    public bool RemoveServerHeader { get; set; } = true;
}

/// <summary>
/// Extension methods for adding security headers middleware to the application pipeline.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds security headers middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="options">Optional configuration for security headers</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, SecurityHeadersOptions? options = null)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>(options ?? new SecurityHeadersOptions());
    }

    /// <summary>
    /// Adds security headers middleware with custom configuration.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="configure">Action to configure security headers options</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, Action<SecurityHeadersOptions> configure)
    {
        var options = new SecurityHeadersOptions();
        configure(options);
        return app.UseMiddleware<SecurityHeadersMiddleware>(options);
    }
}