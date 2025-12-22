using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces.Services;
using Domain.Entities.Audit;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Implementation of audit archive operations using SQL Server partitioning and ledger features.
/// </summary>
public class AuditArchiverService : IAuditArchiver
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditBlobStorage _blobStorage;
    private readonly ILogger<AuditArchiverService> _logger;

    public AuditArchiverService(
        AppDbContext dbContext,
        IAuditBlobStorage blobStorage,
        ILogger<AuditArchiverService> logger)
    {
        _dbContext = dbContext;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    public async Task<AuditArchiveResult> ArchivePartitionAsync(
        DateTime partitionBoundary,
        string archivedBy,
        string? retentionPolicy = null,
        CancellationToken cancellationToken = default)
    {
        // Normalize to first of month
        var boundary = new DateTime(partitionBoundary.Year, partitionBoundary.Month, 1);

        _logger.LogInformation("Starting archive for partition {Boundary}", boundary);

        try
        {
            // Check if already archived
            var existingManifest = await _dbContext.AuditArchiveManifests
                .FirstOrDefaultAsync(m => m.PartitionBoundary == boundary, cancellationToken);

            if (existingManifest != null)
            {
                return AuditArchiveResult.Failed($"Partition {boundary:yyyy-MM} has already been archived");
            }

            // Get partition number for the boundary
            var partitionNumber = await GetPartitionNumberAsync(boundary, cancellationToken);
            if (partitionNumber == null)
            {
                return AuditArchiveResult.Failed($"No partition found for boundary {boundary:yyyy-MM-dd}");
            }

            // Get record count and hash chain endpoints before switching
            var partitionStats = await GetPartitionStatsAsync(boundary, cancellationToken);
            if (partitionStats.RecordCount == 0)
            {
                return AuditArchiveResult.Failed($"Partition {boundary:yyyy-MM} is empty, nothing to archive");
            }

            // Switch partition to staging table (metadata-only operation, instant)
            await SwitchPartitionToStagingAsync(partitionNumber.Value, cancellationToken);

            _logger.LogInformation("Switched partition {Number} to staging, {Count} records",
                partitionNumber, partitionStats.RecordCount);

            // Export staging data to blob storage
            var exportResult = await ExportStagingToBlobAsync(boundary, cancellationToken);
            if (!exportResult.Success)
            {
                // Switch back on failure
                await SwitchPartitionFromStagingAsync(partitionNumber.Value, cancellationToken);
                return AuditArchiveResult.Failed($"Failed to export to blob: {exportResult.ErrorMessage}");
            }

            // Get ledger digest for cryptographic proof
            var ledgerDigest = await GetLedgerDigestAsync(cancellationToken);

            // Create manifest record
            var manifest = AuditArchiveManifest.Create(
                partitionBoundary: boundary,
                firstSequenceNumber: partitionStats.FirstSequenceNumber,
                lastSequenceNumber: partitionStats.LastSequenceNumber,
                recordCount: partitionStats.RecordCount,
                firstRecordHash: partitionStats.FirstRecordHash,
                lastRecordHash: partitionStats.LastRecordHash,
                ledgerDigest: ledgerDigest,
                archiveUri: exportResult.Uri!,
                archiveBlobHash: exportResult.ContentHash!,
                archiveSizeBytes: exportResult.SizeBytes,
                archivedBy: archivedBy,
                retentionPolicy: retentionPolicy);

            _dbContext.AuditArchiveManifests.Add(manifest);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Archive completed for partition {Boundary}, manifest {ManifestId}",
                boundary, manifest.Id);

            return AuditArchiveResult.Succeeded(manifest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive partition {Boundary}", boundary);
            return AuditArchiveResult.Failed($"Archive failed: {ex.Message}");
        }
    }

    public async Task<AuditArchiveResult> PurgePartitionAsync(
        DateTime partitionBoundary,
        CancellationToken cancellationToken = default)
    {
        var boundary = new DateTime(partitionBoundary.Year, partitionBoundary.Month, 1);

        _logger.LogInformation("Starting purge for partition {Boundary}", boundary);

        try
        {
            // Verify manifest exists and is archived
            var manifest = await _dbContext.AuditArchiveManifests
                .FirstOrDefaultAsync(m => m.PartitionBoundary == boundary, cancellationToken);

            if (manifest == null)
            {
                return AuditArchiveResult.Failed($"No archive manifest found for partition {boundary:yyyy-MM}");
            }

            if (manifest.PurgedAt.HasValue)
            {
                return AuditArchiveResult.Failed($"Partition {boundary:yyyy-MM} has already been purged");
            }

            // Verify blob integrity before purging
            var verification = await VerifyArchiveAsync(manifest.Id, cancellationToken);
            if (!verification.IsValid)
            {
                return AuditArchiveResult.Failed($"Archive verification failed: {verification.ErrorMessage}");
            }

            // Truncate staging table partition
            await TruncateStagingPartitionAsync(boundary, cancellationToken);

            // Mark manifest as purged
            manifest.MarkPurged();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Purge completed for partition {Boundary}", boundary);

            return AuditArchiveResult.Succeeded(manifest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to purge partition {Boundary}", boundary);
            return AuditArchiveResult.Failed($"Purge failed: {ex.Message}");
        }
    }

    public async Task AddPartitionBoundaryAsync(
        DateTime boundary,
        CancellationToken cancellationToken = default)
    {
        var normalizedBoundary = new DateTime(boundary.Year, boundary.Month, 1);

        _logger.LogInformation("Adding partition boundary for {Boundary}", normalizedBoundary);

        // Split the partition to add a new boundary
        // This creates a new partition for dates >= boundary
        var sql = $@"
            ALTER PARTITION FUNCTION [AuditLedger_PF]()
            SPLIT RANGE ('{normalizedBoundary:yyyy-MM-dd}');
        ";

        await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);

        _logger.LogInformation("Added partition boundary for {Boundary}", normalizedBoundary);
    }

    public async Task<string> GetLedgerDigestAsync(CancellationToken cancellationToken = default)
    {
        // Get the latest ledger digest from SQL Server
        // This provides cryptographic proof of the ledger state
        var sql = @"
            SELECT TOP 1
                JSON_QUERY(
                    (SELECT
                        database_name,
                        block_id,
                        hash,
                        last_transaction_commit_time
                     FROM sys.database_ledger_digest_locations
                     ORDER BY block_id DESC
                     FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
                ) AS digest;
        ";

        var digest = await _dbContext.Database
            .SqlQueryRaw<string>(sql)
            .FirstOrDefaultAsync(cancellationToken);

        return digest ?? "{}";
    }

    public async Task<ArchiveVerificationResult> VerifyArchiveAsync(
        Guid manifestId,
        CancellationToken cancellationToken = default)
    {
        var manifest = await _dbContext.AuditArchiveManifests
            .FirstOrDefaultAsync(m => m.Id == manifestId, cancellationToken);

        if (manifest == null)
        {
            return ArchiveVerificationResult.Invalid("Manifest not found");
        }

        // Verify blob exists and hash matches
        var blobExists = await _blobStorage.ExistsAsync(manifest.ArchiveUri, cancellationToken);
        if (!blobExists)
        {
            return ArchiveVerificationResult.Invalid("Archive blob not found", blobValid: false);
        }

        var blobHash = await _blobStorage.GetBlobHashAsync(manifest.ArchiveUri, cancellationToken);
        var blobValid = manifest.VerifyBlobIntegrity(blobHash);

        if (!blobValid)
        {
            return ArchiveVerificationResult.Invalid(
                "Blob hash mismatch - archive may be corrupted",
                blobValid: false);
        }

        return ArchiveVerificationResult.Valid();
    }

    public async Task<IReadOnlyList<AuditArchiveManifest>> GetArchiveManifestsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditArchiveManifests.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(m => m.PartitionBoundary >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(m => m.PartitionBoundary <= toDate.Value);
        }

        return await query
            .OrderByDescending(m => m.PartitionBoundary)
            .ToListAsync(cancellationToken);
    }

    #region Private Helper Methods

    private async Task<int?> GetPartitionNumberAsync(DateTime boundary, CancellationToken cancellationToken)
    {
        var sql = $@"
            SELECT $PARTITION.[AuditLedger_PF]('{boundary:yyyy-MM-dd}') AS PartitionNumber;
        ";

        var result = await _dbContext.Database
            .SqlQueryRaw<int>(sql)
            .FirstOrDefaultAsync(cancellationToken);

        return result > 0 ? result : null;
    }

    private async Task<PartitionStats> GetPartitionStatsAsync(DateTime boundary, CancellationToken cancellationToken)
    {
        var nextBoundary = boundary.AddMonths(1);

        var stats = await _dbContext.AuditLedger
            .Where(e => e.OccurredAt >= boundary && e.OccurredAt < nextBoundary)
            .GroupBy(_ => 1)
            .Select(g => new PartitionStats
            {
                RecordCount = g.Count(),
                FirstSequenceNumber = g.Min(e => e.SequenceNumber),
                LastSequenceNumber = g.Max(e => e.SequenceNumber)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (stats == null || stats.RecordCount == 0)
        {
            return new PartitionStats { RecordCount = 0 };
        }

        // Get first and last record hashes
        var firstRecord = await _dbContext.AuditLedger
            .Where(e => e.SequenceNumber == stats.FirstSequenceNumber)
            .Select(e => e.Hash)
            .FirstOrDefaultAsync(cancellationToken);

        var lastRecord = await _dbContext.AuditLedger
            .Where(e => e.SequenceNumber == stats.LastSequenceNumber)
            .Select(e => e.Hash)
            .FirstOrDefaultAsync(cancellationToken);

        stats.FirstRecordHash = firstRecord ?? string.Empty;
        stats.LastRecordHash = lastRecord ?? string.Empty;

        return stats;
    }

    private async Task SwitchPartitionToStagingAsync(int partitionNumber, CancellationToken cancellationToken)
    {
        var sql = $@"
            ALTER TABLE [Audit].[AuditLedger]
            SWITCH PARTITION {partitionNumber}
            TO [Audit].[AuditLedger_Staging] PARTITION {partitionNumber};
        ";

        await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private async Task SwitchPartitionFromStagingAsync(int partitionNumber, CancellationToken cancellationToken)
    {
        var sql = $@"
            ALTER TABLE [Audit].[AuditLedger_Staging]
            SWITCH PARTITION {partitionNumber}
            TO [Audit].[AuditLedger] PARTITION {partitionNumber};
        ";

        await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private async Task<BlobUploadResult> ExportStagingToBlobAsync(
        DateTime boundary,
        CancellationToken cancellationToken)
    {
        var nextBoundary = boundary.AddMonths(1);

        // Query staging table data
        var sql = @"
            SELECT * FROM [Audit].[AuditLedger_Staging]
            WHERE [OccurredAt] >= {0} AND [OccurredAt] < {1}
            ORDER BY [SequenceNumber]
            FOR JSON PATH;
        ";

        var jsonData = await _dbContext.Database
            .SqlQueryRaw<string>(sql, boundary, nextBoundary)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(jsonData))
        {
            return BlobUploadResult.Failed("No data found in staging partition");
        }

        // Convert to stream and upload
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonData));
        return await _blobStorage.UploadAsync(boundary, stream, cancellationToken);
    }

    private async Task TruncateStagingPartitionAsync(DateTime boundary, CancellationToken cancellationToken)
    {
        var partitionNumber = await GetPartitionNumberAsync(boundary, cancellationToken);
        if (partitionNumber == null)
        {
            throw new InvalidOperationException($"Cannot find partition for {boundary:yyyy-MM-dd}");
        }

        var sql = $@"
            TRUNCATE TABLE [Audit].[AuditLedger_Staging]
            WITH (PARTITIONS ({partitionNumber}));
        ";

        await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private class PartitionStats
    {
        public long RecordCount { get; set; }
        public long FirstSequenceNumber { get; set; }
        public long LastSequenceNumber { get; set; }
        public string FirstRecordHash { get; set; } = string.Empty;
        public string LastRecordHash { get; set; } = string.Empty;
    }

    #endregion
}