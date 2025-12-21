using System.ComponentModel.DataAnnotations;

namespace Application.Common.Configuration;

/// <summary>
/// Configuration options for the audit system.
/// </summary>
public class AuditOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Audit";

    /// <summary>
    /// The processing mode for audit entries.
    /// </summary>
    public AuditProcessingMode ProcessingMode { get; set; } = AuditProcessingMode.Sync;

    /// <summary>
    /// Maximum number of entries to batch before flushing (for Batched mode).
    /// </summary>
    [Range(1, 1000)]
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Maximum time to wait before flushing a partial batch in milliseconds (for Batched mode).
    /// </summary>
    [Range(100, 60000)]
    public int FlushIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Whether to log audit events to the console for debugging.
    /// </summary>
    public bool EnableConsoleLogging { get; set; } = false;
}

/// <summary>
/// Processing mode for audit entries.
/// </summary>
public enum AuditProcessingMode
{
    /// <summary>
    /// Process audit entries synchronously in the same request.
    /// Most reliable, but adds latency to requests.
    /// </summary>
    Sync,

    /// <summary>
    /// Queue audit entries and process in batches via background service.
    /// Better performance, slight delay in audit entries appearing.
    /// </summary>
    Batched
}