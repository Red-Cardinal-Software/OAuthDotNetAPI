using Domain.Entities.Audit;

namespace Application.Interfaces.Services;

/// <summary>
/// Service for archiving audit ledger partitions with cryptographic chain of custody.
/// Implements the partition switch/export/purge workflow for compliance retention.
/// </summary>
public interface IAuditArchiver
{
    /// <summary>
    /// Archives a partition by switching it to staging, exporting to blob storage,
    /// recording the ledger digest, and tracking in the manifest.
    /// </summary>
    /// <param name="partitionBoundary">The first day of the month to archive</param>
    /// <param name="archivedBy">User or service account performing the archive</param>
    /// <param name="retentionPolicy">Optional retention policy name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with the created manifest or error</returns>
    Task<AuditArchiveResult> ArchivePartitionAsync(
        DateTime partitionBoundary,
        string archivedBy,
        string? retentionPolicy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges a partition from the staging table after successful archive.
    /// This is a separate step to allow verification before deletion.
    /// </summary>
    /// <param name="partitionBoundary">The partition to purge</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or error</returns>
    Task<AuditArchiveResult> PurgePartitionAsync(
        DateTime partitionBoundary,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new partition boundary for the next month.
    /// Should be called by a scheduled job before each month.
    /// </summary>
    /// <param name="boundary">The new partition boundary date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddPartitionBoundaryAsync(
        DateTime boundary,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the SQL Server ledger digest for verification.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ledger digest JSON</returns>
    Task<string> GetLedgerDigestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies an archived partition by comparing the blob hash with the manifest.
    /// </summary>
    /// <param name="manifestId">The manifest record to verify</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result</returns>
    Task<ArchiveVerificationResult> VerifyArchiveAsync(
        Guid manifestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all archive manifests, optionally filtered.
    /// </summary>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of archive manifests</returns>
    Task<IReadOnlyList<AuditArchiveManifest>> GetArchiveManifestsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an archive operation.
/// </summary>
public record AuditArchiveResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public AuditArchiveManifest? Manifest { get; init; }

    public static AuditArchiveResult Succeeded(AuditArchiveManifest manifest)
        => new() { Success = true, Manifest = manifest };

    public static AuditArchiveResult Failed(string errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Result of archive verification.
/// </summary>
public record ArchiveVerificationResult
{
    public bool IsValid { get; init; }
    public bool BlobIntegrityValid { get; init; }
    public bool HashChainValid { get; init; }
    public string? ErrorMessage { get; init; }

    public static ArchiveVerificationResult Valid()
        => new() { IsValid = true, BlobIntegrityValid = true, HashChainValid = true };

    public static ArchiveVerificationResult Invalid(string errorMessage, bool blobValid = false, bool hashValid = false)
        => new() { IsValid = false, BlobIntegrityValid = blobValid, HashChainValid = hashValid, ErrorMessage = errorMessage };
}