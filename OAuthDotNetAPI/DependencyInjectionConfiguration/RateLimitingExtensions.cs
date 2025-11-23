using System.Globalization;
using System.Threading.RateLimiting;
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
            // Policy for authentication endpoints (login, refresh) - most restrictive
            var authPermitLimit = configuration.GetValue("RateLimiting-Auth-PermitLimit", 
                SystemDefaults.DefaultRateLimitAuthPermitLimit);
            var authWindowMinutes = configuration.GetValue("RateLimiting-Auth-WindowMinutes", 
                SystemDefaults.DefaultRateLimitAuthWindowMinutes);

            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.PermitLimit = authPermitLimit;
                opt.Window = TimeSpan.FromMinutes(authWindowMinutes);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0; // No queuing - reject immediately when limit exceeded
            });

            // Policy for password reset - moderate restrictions to prevent email spam
            var passwordResetPermitLimit = configuration.GetValue("RateLimiting-PasswordReset-PermitLimit", 
                SystemDefaults.DefaultRateLimitPasswordResetPermitLimit);
            var passwordResetWindowMinutes = configuration.GetValue("RateLimiting-PasswordReset-WindowMinutes", 
                SystemDefaults.DefaultRateLimitPasswordResetWindowMinutes);

            options.AddFixedWindowLimiter("password-reset", opt =>
            {
                opt.PermitLimit = passwordResetPermitLimit;
                opt.Window = TimeSpan.FromMinutes(passwordResetWindowMinutes);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Policy for general API endpoints - more permissive
            var apiPermitLimit = configuration.GetValue("RateLimiting-Api-PermitLimit", 
                SystemDefaults.DefaultRateLimitApiPermitLimit);
            var apiWindowMinutes = configuration.GetValue("RateLimiting-Api-WindowMinutes", 
                SystemDefaults.DefaultRateLimitApiWindowMinutes);

            options.AddFixedWindowLimiter("api", opt =>
            {
                opt.PermitLimit = apiPermitLimit;
                opt.Window = TimeSpan.FromMinutes(apiWindowMinutes);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Policy for health check endpoints - moderate limits to prevent abuse
            var healthPermitLimit = configuration.GetValue("RateLimiting-Health-PermitLimit", 30);
            var healthWindowMinutes = configuration.GetValue("RateLimiting-Health-WindowMinutes", 1);

            options.AddFixedWindowLimiter("health", opt =>
            {
                opt.PermitLimit = healthPermitLimit;
                opt.Window = TimeSpan.FromMinutes(healthWindowMinutes);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Global rate limiter - prevents any single IP from overwhelming the API
            var globalPermitLimit = configuration.GetValue("RateLimiting-Global-PermitLimit", 
                SystemDefaults.DefaultRateLimitGlobalPermitLimit);
            var globalWindowMinutes = configuration.GetValue("RateLimiting-Global-WindowMinutes", 
                SystemDefaults.DefaultRateLimitGlobalWindowMinutes);

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Partition by IP address
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = globalPermitLimit,
                    Window = TimeSpan.FromMinutes(globalWindowMinutes),
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