namespace Application.DTOs.Audit;

/// <summary>
/// DTO for viewing audit archive manifest information.
/// </summary>
public class ArchiveManifestDto
{
    /// <summary>
    /// Unique identifier for this archive record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The partition boundary value (first day of the archived month).
    /// </summary>
    public DateTime PartitionBoundary { get; set; }

    /// <summary>
    /// First sequence number in this partition.
    /// </summary>
    public long FirstSequenceNumber { get; set; }

    /// <summary>
    /// Last sequence number in this partition.
    /// </summary>
    public long LastSequenceNumber { get; set; }

    /// <summary>
    /// Total number of records archived.
    /// </summary>
    public long RecordCount { get; set; }

    /// <summary>
    /// SHA-256 hash of the first record in partition (chain start).
    /// </summary>
    public string FirstRecordHash { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of the last record in partition (chain end).
    /// </summary>
    public string LastRecordHash { get; set; } = string.Empty;

    /// <summary>
    /// URI to the archived data in immutable blob storage.
    /// </summary>
    public string ArchiveUri { get; set; } = string.Empty;

    /// <summary>
    /// Size of the archived blob in bytes.
    /// </summary>
    public long ArchiveSizeBytes { get; set; }

    /// <summary>
    /// When the archive was created.
    /// </summary>
    public DateTime ArchivedAt { get; set; }

    /// <summary>
    /// User or service account that performed the archive.
    /// </summary>
    public string ArchivedBy { get; set; } = string.Empty;

    /// <summary>
    /// When this partition was purged from the main table (null if not yet purged).
    /// </summary>
    public DateTime? PurgedAt { get; set; }

    /// <summary>
    /// Retention policy that triggered this archive.
    /// </summary>
    public string? RetentionPolicy { get; set; }
}