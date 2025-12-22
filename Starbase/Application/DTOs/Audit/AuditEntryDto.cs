using Domain.Entities.Audit;

namespace Application.DTOs.Audit;

/// <summary>
/// DTO for reading audit ledger entries.
/// </summary>
public class AuditEntryDto
{
    public long SequenceNumber { get; set; }
    public Guid EventId { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Hash { get; set; } = string.Empty;

    // Who
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string? IpAddress { get; set; }

    // What
    public AuditEventType EventType { get; set; }
    public AuditAction Action { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }

    // Outcome
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
}