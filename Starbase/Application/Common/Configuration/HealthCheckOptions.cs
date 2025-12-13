using System.ComponentModel.DataAnnotations;

namespace Application.Common.Configuration;

/// <summary>
/// Configuration options for health check monitoring.
/// Controls which health checks are enabled and their behavior.
/// </summary>
public class HealthCheckOptions
{
    /// <summary>
    /// Gets the configuration section name for health check options.
    /// </summary>
    public const string SectionName = "HealthChecks";

    /// <summary>
    /// Gets or sets whether to include memory health checks.
    /// Memory checks are considered privileged and may expose system information.
    /// </summary>
    public bool IncludeMemoryCheck { get; set; } = false;

    /// <summary>
    /// Gets or sets the memory threshold in MB for degraded status.
    /// Only used when IncludeMemoryCheck is true.
    /// </summary>
    [Range(100, 8192, ErrorMessage = "Memory threshold must be between 100 and 8192 MB")]
    public int MemoryThresholdMB { get; set; } = 512;

    /// <summary>
    /// Gets or sets the memory threshold in MB for unhealthy status.
    /// Only used when IncludeMemoryCheck is true.
    /// </summary>
    [Range(200, 16384, ErrorMessage = "Memory unhealthy threshold must be between 200 and 16384 MB")]
    public int MemoryUnhealthyThresholdMB { get; set; } = 1024;
}
