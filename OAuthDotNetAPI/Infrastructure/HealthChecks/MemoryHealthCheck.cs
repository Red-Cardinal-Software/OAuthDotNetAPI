using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Infrastructure.HealthChecks
{
    public class MemoryHealthCheck(IConfiguration configuration, ILogger<MemoryHealthCheck> logger) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Get threshold from configuration, default to 1GB if not specified
            var thresholdMb = configuration.GetValue("HealthChecks:MemoryThresholdMB", 1024);
            var threshold = (long)thresholdMb * 1024 * 1024;
            
            var memoryInfo = GC.GetTotalMemory(false);
            
            logger.LogDebug("Memory health check: {CurrentMB} MB used, threshold: {ThresholdMB} MB", 
                memoryInfo / 1024 / 1024, thresholdMb);
            
            return Task.FromResult(memoryInfo < threshold
                ? HealthCheckResult.Healthy("Service operational")
                : HealthCheckResult.Degraded("Service degraded"));
        }
    }
}