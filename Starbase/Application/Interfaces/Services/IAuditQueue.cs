using Application.DTOs.Audit;

namespace Application.Interfaces.Services;

/// <summary>
/// Interface for queueing audit entries for background processing.
/// </summary>
public interface IAuditQueue
{
    /// <summary>
    /// Enqueue an audit entry for background processing.
    /// </summary>
    /// <param name="entry">The audit entry to queue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask EnqueueAsync(CreateAuditEntryDto entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeue audit entries for processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of audit entries.</returns>
    IAsyncEnumerable<CreateAuditEntryDto> DequeueAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current count of queued entries.
    /// </summary>
    int Count { get; }
}