using MediatR;

namespace Application.Events;

/// <summary>
/// Marker interface for domain events.
/// All domain events should implement this interface to be published via MediatR.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// The timestamp when the event occurred.
    /// </summary>
    DateTimeOffset OccurredAt { get; }

    /// <summary>
    /// Correlation ID for tracing related events across services.
    /// </summary>
    string? CorrelationId { get; }
}