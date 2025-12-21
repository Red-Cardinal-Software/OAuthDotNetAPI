namespace Domain.Entities.Audit;

/// <summary>
/// Tracks archived audit ledger partitions for compliance and chain of custody.
/// Each record represents a partition that has been exported and purged.
/// </summary>
public class AuditArchiveManifest
{
    /// <summary>
    /// Unique identifier for this archive record.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The partition boundary value (first day of the archived month).
    /// </summary>
    public DateTime PartitionBoundary { get; private set; }

    /// <summary>
    /// First sequence number in this partition.
    /// </summary>
    public long FirstSequenceNumber { get; private set; }

    /// <summary>
    /// Last sequence number in this partition.
    /// </summary>
    public long LastSequenceNumber { get; private set; }

    /// <summary>
    /// Total number of records archived.
    /// </summary>
    public long RecordCount { get; private set; }

    /// <summary>
    /// SHA-256 hash of the first record in partition (chain start).
    /// </summary>
    public string FirstRecordHash { get; private set; } = null!;

    /// <summary>
    /// SHA-256 hash of the last record in partition (chain end).
    /// </summary>
    public string LastRecordHash { get; private set; } = null!;

    /// <summary>
    /// SQL Server Ledger digest at time of archive (cryptographic proof).
    /// </summary>
    public string LedgerDigest { get; private set; } = null!;

    /// <summary>
    /// URI to the archived data in immutable blob storage.
    /// </summary>
    public string ArchiveUri { get; private set; } = null!;

    /// <summary>
    /// SHA-256 hash of the archived blob for integrity verification.
    /// </summary>
    public string ArchiveBlobHash { get; private set; } = null!;

    /// <summary>
    /// Size of the archived blob in bytes.
    /// </summary>
    public long ArchiveSizeBytes { get; private set; }

    /// <summary>
    /// When the archive was created.
    /// </summary>
    public DateTime ArchivedAt { get; private set; }

    /// <summary>
    /// User or service account that performed the archive.
    /// </summary>
    public string ArchivedBy { get; private set; } = null!;

    /// <summary>
    /// When this partition was purged from the main table.
    /// </summary>
    public DateTime? PurgedAt { get; private set; }

    /// <summary>
    /// Retention policy that triggered this archive (e.g., "90-day-rolling").
    /// </summary>
    public string? RetentionPolicy { get; private set; }

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private AuditArchiveManifest()
    {
    }

    /// <summary>
    /// Creates a new archive manifest record.
    /// </summary>
    public static AuditArchiveManifest Create(
        DateTime partitionBoundary,
        long firstSequenceNumber,
        long lastSequenceNumber,
        long recordCount,
        string firstRecordHash,
        string lastRecordHash,
        string ledgerDigest,
        string archiveUri,
        string archiveBlobHash,
        long archiveSizeBytes,
        string archivedBy,
        string? retentionPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(firstRecordHash);
        ArgumentNullException.ThrowIfNull(lastRecordHash);
        ArgumentNullException.ThrowIfNull(ledgerDigest);
        ArgumentNullException.ThrowIfNull(archiveUri);
        ArgumentNullException.ThrowIfNull(archiveBlobHash);
        ArgumentNullException.ThrowIfNull(archivedBy);

        if (firstSequenceNumber > lastSequenceNumber)
            throw new ArgumentException("First sequence number cannot be greater than last sequence number");

        if (recordCount <= 0)
            throw new ArgumentException("Record count must be positive");

        return new AuditArchiveManifest
        {
            Id = Guid.NewGuid(),
            PartitionBoundary = partitionBoundary.Date,
            FirstSequenceNumber = firstSequenceNumber,
            LastSequenceNumber = lastSequenceNumber,
            RecordCount = recordCount,
            FirstRecordHash = firstRecordHash,
            LastRecordHash = lastRecordHash,
            LedgerDigest = ledgerDigest,
            ArchiveUri = archiveUri,
            ArchiveBlobHash = archiveBlobHash,
            ArchiveSizeBytes = archiveSizeBytes,
            ArchivedAt = DateTime.UtcNow,
            ArchivedBy = archivedBy,
            RetentionPolicy = retentionPolicy
        };
    }

    /// <summary>
    /// Records that this partition has been purged from the main table.
    /// </summary>
    public void MarkPurged()
    {
        if (PurgedAt.HasValue)
            throw new InvalidOperationException("Partition has already been purged");

        PurgedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Verifies that a blob hash matches the recorded archive hash.
    /// </summary>
    public bool VerifyBlobIntegrity(string blobHash)
    {
        return string.Equals(ArchiveBlobHash, blobHash, StringComparison.OrdinalIgnoreCase);
    }
}