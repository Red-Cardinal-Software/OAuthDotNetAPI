using Domain.Entities.Audit;

namespace Application.DTOs.Audit;

/// <summary>
/// Query parameters for searching audit entries.
/// </summary>
public class AuditQueryDto
{
    /// <summary>
    /// Filter by user ID.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Filter by event type.
    /// </summary>
    public AuditEventType? EventType { get; set; }

    /// <summary>
    /// Filter by specific action.
    /// </summary>
    public AuditAction? Action { get; set; }

    /// <summary>
    /// Filter by entity type.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Filter by entity ID.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Filter by success/failure.
    /// </summary>
    public bool? Success { get; set; }

    /// <summary>
    /// Start of date range (inclusive).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// End of date range (inclusive).
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size (max 100).
    /// </summary>
    public int PageSize { get; set; } = 50;
}