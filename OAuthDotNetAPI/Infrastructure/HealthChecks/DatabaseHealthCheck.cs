using Microsoft.Extensions.Diagnostics.HealthChecks;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace Infrastructure.HealthChecks
{
    public class DatabaseHealthCheck(IServiceProvider serviceProvider, ILogger<DatabaseHealthCheck> logger) : IHealthCheck
    {
        private const int WarningThresholdMs = 1000;
        private const int TimeoutSeconds = 5;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

                // Create a new scope to get a scoped DbContext
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Use a simple query with timeout
                var canConnect = await dbContext.Database
                    .CanConnectAsync(timeoutCts.Token);
                
                if (!canConnect)
                {
                    logger.LogError("Database CanConnectAsync returned false");
                    return HealthCheckResult.Unhealthy("Service unavailable");
                }

                // Optional: Run a lightweight query to verify read capability
                await dbContext.Database
                    .ExecuteSqlRawAsync("SELECT 1", timeoutCts.Token);

                stopwatch.Stop();

                // Log detailed information internally
                logger.LogInformation("Database health check completed in {ElapsedMs}ms", 
                    stopwatch.ElapsedMilliseconds);

                // Determine health status based on response time
                if (stopwatch.ElapsedMilliseconds > WarningThresholdMs)
                {
                    logger.LogWarning("Database health check took {ElapsedMs}ms, exceeding warning threshold of {Threshold}ms", 
                        stopwatch.ElapsedMilliseconds, WarningThresholdMs);
                    
                    return HealthCheckResult.Degraded("Service degraded");
                }

                return HealthCheckResult.Healthy("Service operational");
            }
            catch (OperationCanceledException)
            {
                logger.LogError("Database health check timed out after {TimeoutSeconds} seconds", TimeoutSeconds);
                
                return HealthCheckResult.Unhealthy("Service unavailable");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database health check failed with exception: {ExceptionType}", ex.GetType().Name);
                
                return HealthCheckResult.Unhealthy("Service unavailable");
            }
        }
    }
}