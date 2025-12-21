using Domain.Entities.Audit;

namespace Application.DTOs.Audit;

/// <summary>
/// DTO for creating a new audit ledger entry.
/// The service will handle sequence numbers, hashing, and timestamps.
/// </summary>
public class CreateAuditEntryDto
{
    /// <summary>
    /// Category of the audit event.
    /// </summary>
    public required AuditEventType EventType { get; init; }

    /// <summary>
    /// Specific action performed.
    /// </summary>
    public required AuditAction Action { get; init; }

    /// <summary>
    /// Whether the action succeeded.
    /// </summary>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Reason for failure if Success is false.
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// User ID who performed the action (null for system events).
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Username at the time of the event.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Client IP address.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Client user agent string.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Correlation ID for tracing across services.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Type of entity affected (e.g., "User", "Role", "MfaMethod").
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// ID of the affected entity.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// JSON snapshot of previous values (for updates/deletes).
    /// </summary>
    public string? OldValues { get; init; }

    /// <summary>
    /// JSON snapshot of new values (for creates/updates).
    /// </summary>
    public string? NewValues { get; init; }

    /// <summary>
    /// Additional contextual data as JSON.
    /// </summary>
    public string? AdditionalData { get; init; }
}