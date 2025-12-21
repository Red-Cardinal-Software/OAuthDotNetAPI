using System.Globalization;
using System.Threading.RateLimiting;
using Application.Common.Configuration;
using Application.Common.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjectionConfiguration;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            // Read strongly-typed configuration instead of raw config values
            // This provides validation and better maintainability
            var rateLimitOptions = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();

            // Policy for authentication endpoints (login, refresh) - most restrictive
            // Partitioned by IP to prevent one attacker from blocking all users
            options.AddPolicy("auth", context =>
            {
                var ip = GetClientIpAddress(context);
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitOptions.Auth.PermitLimit,
                    Window = TimeSpan.FromMinutes(rateLimitOptions.Auth.WindowMinutes),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0 // No queuing - reject immediately when limit exceeded
                });
            });

            // Policy for password reset - moderate restrictions to prevent email spam
            options.AddPolicy("password-reset", context =>
            {
                var ip = GetClientIpAddress(context);
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitOptions.PasswordReset.PermitLimit,
                    Window = TimeSpan.FromMinutes(rateLimitOptions.PasswordReset.WindowMinutes),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });

            // Policy for general API endpoints - more permissive
            options.AddPolicy("api", context =>
            {
                var ip = GetClientIpAddress(context);
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitOptions.Api.PermitLimit,
                    Window = TimeSpan.FromMinutes(rateLimitOptions.Api.WindowMinutes),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });

            // Policy for health check endpoints - moderate limits to prevent abuse
            options.AddPolicy("health", context =>
            {
                var ip = GetClientIpAddress(context);
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitOptions.Health.PermitLimit,
                    Window = TimeSpan.FromMinutes(rateLimitOptions.Health.WindowMinutes),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });

            // Policy for MFA setup endpoints - restrictive to prevent brute force attacks
            options.AddPolicy("mfa-setup", context =>
            {
                var ip = GetClientIpAddress(context);
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitOptions.MfaSetup.PermitLimit,
                    Window = TimeSpan.FromMinutes(rateLimitOptions.MfaSetup.WindowMinutes),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });

            // Global rate limiter - prevents any single IP from overwhelming the API
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var ip = GetClientIpAddress(context);
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitOptions.Global.PermitLimit,
                    Window = TimeSpan.FromMinutes(rateLimitOptions.Global.WindowMinutes),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });

            // Customize the response when rate limit is exceeded
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                TimeSpan? retryAfter = null;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                {
                    retryAfter = retry;
                    context.HttpContext.Response.Headers.RetryAfter = retry.TotalSeconds.ToString(CultureInfo.InvariantCulture);
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests. Please try again later.",
                    retryAfter = retryAfter?.TotalSeconds
                }, cancellationToken);
            };
        });

        return services;
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        return app.UseRateLimiter();
    }

    /// <summary>
    /// Gets the client IP address, checking for forwarded headers (X-Forwarded-For)
    /// when behind a reverse proxy like nginx or a load balancer.
    /// </summary>
    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for X-Forwarded-For header (set by reverse proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs: client, proxy1, proxy2
            // The first one is the original client IP
            var ip = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
                return ip;
        }

        // Fall back to the direct connection IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}