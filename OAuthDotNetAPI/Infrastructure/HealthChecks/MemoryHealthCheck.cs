using Application.Common.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.HealthChecks
{
    public class MemoryHealthCheck(IOptions<HealthCheckOptions> healthCheckOptions, ILogger<MemoryHealthCheck> logger) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var options = healthCheckOptions.Value;
            var currentMemoryMb = GC.GetTotalMemory(false) / 1024 / 1024;

            logger.LogDebug("Memory health check: {CurrentMB} MB used, degraded threshold: {DegradedMB} MB, unhealthy threshold: {UnhealthyMB} MB",
                currentMemoryMb, options.MemoryThresholdMB, options.MemoryUnhealthyThresholdMB);

            // Check against unhealthy threshold first (higher priority)
            if (currentMemoryMb >= options.MemoryUnhealthyThresholdMB)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"Memory usage is critical: {currentMemoryMb} MB"));
            }

            // Check against degraded threshold
            if (currentMemoryMb >= options.MemoryThresholdMB)
            {
                return Task.FromResult(HealthCheckResult.Degraded($"Memory usage is elevated: {currentMemoryMb} MB"));
            }

            return Task.FromResult(HealthCheckResult.Healthy($"Memory usage is normal: {currentMemoryMb} MB"));
        }
    }
}