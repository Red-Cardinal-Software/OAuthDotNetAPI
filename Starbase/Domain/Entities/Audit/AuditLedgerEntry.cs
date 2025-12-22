namespace Domain.Entities.Audit;

/// <summary>
/// Immutable audit ledger entry with cryptographic hash chain for tamper detection.
/// Append-only design for compliance requirements (SOC2, HIPAA, PCI-DSS).
/// </summary>
public class AuditLedgerEntry
{
    /// <summary>
    /// Sequential ledger number for ordering and gap detection.
    /// </summary>
    public long SequenceNumber { get; init; }

    /// <summary>
    /// Unique event identifier (GUID).
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// When the event occurred (UTC).
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Hash of the previous entry for tamper detection.
    /// First entry uses a genesis hash.
    /// </summary>
    public string PreviousHash { get; init; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of this entry's content + previous hash.
    /// </summary>
    public string Hash { get; init; } = string.Empty;

    #region Who

    /// <summary>
    /// User ID who performed the action (null for system events).
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Username at the time of the event (denormalized for historical accuracy).
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

    #endregion

    #region What

    /// <summary>
    /// Category of the audit event.
    /// </summary>
    public AuditEventType EventType { get; init; }

    /// <summary>
    /// Specific action performed.
    /// </summary>
    public AuditAction Action { get; init; }

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

    #endregion

    #region Outcome

    /// <summary>
    /// Whether the action succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Reason for failure if Success is false.
    /// </summary>
    public string? FailureReason { get; init; }

    #endregion

    #region Outbox

    /// <summary>
    /// Whether this entry has been dispatched to external systems.
    /// </summary>
    public bool Dispatched { get; set; }

    /// <summary>
    /// When this entry was dispatched (UTC).
    /// </summary>
    public DateTime? DispatchedAt { get; set; }

    #endregion
}