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
            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.PermitLimit = rateLimitOptions.Auth.PermitLimit;
                opt.Window = TimeSpan.FromMinutes(rateLimitOptions.Auth.WindowMinutes);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0; // No queuing - reject immediately when limit exceeded
            });

            // Policy for password reset - moderate restrictions to prevent email spam
            options.AddFixedWindowLimiter("password-reset", opt =>
            {
                opt.PermitLimit = rateLimitOptions.PasswordReset.PermitLimit;
                opt.Window = TimeSpan.FromMinutes(rateLimitOptions.PasswordReset.WindowMinutes);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Policy for general API endpoints - more permissive
            options.AddFixedWindowLimiter("api", opt =>
            {
                opt.PermitLimit = rateLimitOptions.Api.PermitLimit;
                opt.Window = TimeSpan.FromMinutes(rateLimitOptions.Api.WindowMinutes);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Policy for health check endpoints - moderate limits to prevent abuse
            options.AddFixedWindowLimiter("health", opt =>
            {
                opt.PermitLimit = rateLimitOptions.Health.PermitLimit;
                opt.Window = TimeSpan.FromMinutes(rateLimitOptions.Health.WindowMinutes);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Policy for MFA setup endpoints - restrictive to prevent brute force attacks
            options.AddFixedWindowLimiter("mfa-setup", opt =>
            {
                opt.PermitLimit = rateLimitOptions.MfaSetup.PermitLimit;
                opt.Window = TimeSpan.FromMinutes(rateLimitOptions.MfaSetup.WindowMinutes);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Global rate limiter - prevents any single IP from overwhelming the API
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Partition by IP address
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
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
}