using Domain.Entities.Audit;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository for audit ledger entries.
/// Provides append-only operations with hash chain integrity.
/// </summary>
public interface IAuditLedgerRepository
{
    /// <summary>
    /// Gets the next sequence number for the ledger.
    /// </summary>
    /// <returns>The next sequence number.</returns>
    Task<long> GetNextSequenceNumberAsync();

    /// <summary>
    /// Gets the hash of the last entry for chain continuity.
    /// </summary>
    /// <returns>The hash of the last entry, or genesis hash if empty.</returns>
    Task<string> GetLastHashAsync();

    /// <summary>
    /// Appends entries to the ledger.
    /// </summary>
    /// <param name="entries">The entries to append.</param>
    Task AppendAsync(IEnumerable<AuditLedgerEntry> entries);

    /// <summary>
    /// Queries entries with filtering.
    /// </summary>
    /// <param name="predicate">Filter predicate.</param>
    /// <param name="skip">Number of entries to skip.</param>
    /// <param name="take">Number of entries to take.</param>
    /// <returns>Filtered entries.</returns>
    Task<(List<AuditLedgerEntry> Items, int TotalCount)> QueryAsync(
        Func<IQueryable<AuditLedgerEntry>, IQueryable<AuditLedgerEntry>> predicate,
        int skip,
        int take);

    /// <summary>
    /// Gets entries in a sequence range for verification.
    /// </summary>
    /// <param name="fromSequence">Starting sequence (inclusive).</param>
    /// <param name="toSequence">Ending sequence (inclusive).</param>
    /// <returns>Entries in the range.</returns>
    Task<List<AuditLedgerEntry>> GetRangeAsync(long fromSequence, long toSequence);

    /// <summary>
    /// Gets undispatched entries for the outbox pattern.
    /// </summary>
    /// <param name="batchSize">Maximum entries to return.</param>
    /// <returns>Undispatched entries.</returns>
    Task<List<AuditLedgerEntry>> GetUndispatchedAsync(int batchSize);

    /// <summary>
    /// Marks entries as dispatched.
    /// </summary>
    /// <param name="sequenceNumbers">Sequence numbers to mark.</param>
    Task MarkDispatchedAsync(IEnumerable<long> sequenceNumbers);

    /// <summary>
    /// Gets the minimum and maximum sequence numbers.
    /// </summary>
    /// <returns>Tuple of (min, max) sequence numbers.</returns>
    Task<(long Min, long Max)> GetSequenceRangeAsync();
}