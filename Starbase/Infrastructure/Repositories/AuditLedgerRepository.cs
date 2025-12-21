using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for the audit ledger.
/// Provides append-only operations with hash chain support.
/// </summary>
public class AuditLedgerRepository(ICrudOperator<AuditLedgerEntry> crudOperator) : IAuditLedgerRepository
{
    /// <summary>
    /// Genesis hash for the first entry in the ledger.
    /// </summary>
    private const string GenesisHash = "0000000000000000000000000000000000000000000000000000000000000000";

    /// <inheritdoc />
    public async Task<long> GetNextSequenceNumberAsync()
    {
        var maxSequence = await crudOperator.GetAll()
            .MaxAsync(e => (long?)e.SequenceNumber);

        return (maxSequence ?? 0) + 1;
    }

    /// <inheritdoc />
    public async Task<string> GetLastHashAsync()
    {
        var lastEntry = await crudOperator.GetAll()
            .OrderByDescending(e => e.SequenceNumber)
            .Select(e => e.Hash)
            .FirstOrDefaultAsync();

        return lastEntry ?? GenesisHash;
    }

    /// <inheritdoc />
    public async Task AppendAsync(IEnumerable<AuditLedgerEntry> entries)
    {
        await crudOperator.AddManyAsync(entries);
    }

    /// <inheritdoc />
    public async Task<(List<AuditLedgerEntry> Items, int TotalCount)> QueryAsync(
        Func<IQueryable<AuditLedgerEntry>, IQueryable<AuditLedgerEntry>> predicate,
        int skip,
        int take)
    {
        var query = predicate(crudOperator.GetAll());

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<List<AuditLedgerEntry>> GetRangeAsync(long fromSequence, long toSequence)
    {
        return await crudOperator.GetAll()
            .Where(e => e.SequenceNumber >= fromSequence && e.SequenceNumber <= toSequence)
            .OrderBy(e => e.SequenceNumber)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<AuditLedgerEntry>> GetUndispatchedAsync(int batchSize)
    {
        return await crudOperator.GetAll()
            .Where(e => !e.Dispatched)
            .OrderBy(e => e.SequenceNumber)
            .Take(batchSize)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task MarkDispatchedAsync(IEnumerable<long> sequenceNumbers)
    {
        var sequenceList = sequenceNumbers.ToList();
        if (sequenceList.Count == 0) return;

        var entries = await crudOperator.GetAll()
            .Where(e => sequenceList.Contains(e.SequenceNumber))
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var entry in entries)
        {
            entry.Dispatched = true;
            entry.DispatchedAt = now;
        }
    }

    /// <inheritdoc />
    public async Task<(long Min, long Max)> GetSequenceRangeAsync()
    {
        var query = crudOperator.GetAll();

        var min = await query.MinAsync(e => (long?)e.SequenceNumber) ?? 0;
        var max = await query.MaxAsync(e => (long?)e.SequenceNumber) ?? 0;

        return (min, max);
    }
}