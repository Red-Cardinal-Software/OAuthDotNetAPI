using Application.DTOs.Audit;
using Application.Models;

namespace Application.Interfaces.Services;

/// <summary>
/// Service for managing the audit ledger with hash chain integrity.
/// Provides append-only audit logging for compliance requirements.
/// </summary>
public interface IAuditLedger
{
    /// <summary>
    /// Records a new audit entry to the ledger.
    /// Automatically assigns sequence number and computes hash chain.
    /// </summary>
    /// <param name="entry">The audit entry details.</param>
    /// <returns>The created audit entry with sequence number and hash.</returns>
    Task<ServiceResponse<AuditEntryDto>> RecordAsync(CreateAuditEntryDto entry);

    /// <summary>
    /// Records multiple audit entries in a single transaction.
    /// Useful for batch operations that need atomic recording.
    /// </summary>
    /// <param name="entries">The audit entries to record.</param>
    /// <returns>The created audit entries with sequence numbers and hashes.</returns>
    Task<ServiceResponse<List<AuditEntryDto>>> RecordBatchAsync(IEnumerable<CreateAuditEntryDto> entries);

    /// <summary>
    /// Queries audit entries with optional filtering.
    /// </summary>
    /// <param name="query">Query parameters for filtering.</param>
    /// <returns>Paginated list of audit entries.</returns>
    Task<ServiceResponse<PagedResult<AuditEntryDto>>> QueryAsync(AuditQueryDto query);

    /// <summary>
    /// Gets audit entries for a specific entity.
    /// </summary>
    /// <param name="entityType">The entity type (e.g., "User", "Role").</param>
    /// <param name="entityId">The entity ID.</param>
    /// <returns>Audit trail for the entity.</returns>
    Task<ServiceResponse<List<AuditEntryDto>>> GetEntityHistoryAsync(string entityType, string entityId);

    /// <summary>
    /// Gets audit entries for a specific user's actions.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <returns>Audit trail for the user's actions.</returns>
    Task<ServiceResponse<List<AuditEntryDto>>> GetUserActivityAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Verifies the hash chain integrity of the audit ledger.
    /// </summary>
    /// <param name="fromSequence">Starting sequence number (optional).</param>
    /// <param name="toSequence">Ending sequence number (optional).</param>
    /// <returns>Verification result with any detected issues.</returns>
    Task<ServiceResponse<LedgerVerificationResult>> VerifyIntegrityAsync(
        long? fromSequence = null,
        long? toSequence = null);

    /// <summary>
    /// Gets undispatched entries for the outbox pattern.
    /// </summary>
    /// <param name="batchSize">Maximum number of entries to return.</param>
    /// <returns>Undispatched audit entries.</returns>
    Task<ServiceResponse<List<AuditEntryDto>>> GetUndispatchedAsync(int batchSize = 100);

    /// <summary>
    /// Marks entries as dispatched after successful delivery to external systems.
    /// </summary>
    /// <param name="sequenceNumbers">Sequence numbers of entries to mark as dispatched.</param>
    /// <returns>Success result.</returns>
    Task<ServiceResponse<bool>> MarkDispatchedAsync(IEnumerable<long> sequenceNumbers);
}

/// <summary>
/// Result of ledger integrity verification.
/// </summary>
public class LedgerVerificationResult
{
    /// <summary>
    /// Whether the ledger passed integrity verification.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Total entries verified.
    /// </summary>
    public long EntriesVerified { get; set; }

    /// <summary>
    /// First sequence number in the verified range.
    /// </summary>
    public long FirstSequence { get; set; }

    /// <summary>
    /// Last sequence number in the verified range.
    /// </summary>
    public long LastSequence { get; set; }

    /// <summary>
    /// List of any issues found during verification.
    /// </summary>
    public List<LedgerIssue> Issues { get; set; } = [];
}

/// <summary>
/// An issue detected during ledger verification.
/// </summary>
public class LedgerIssue
{
    /// <summary>
    /// Sequence number where the issue was detected.
    /// </summary>
    public long SequenceNumber { get; set; }

    /// <summary>
    /// Type of issue detected.
    /// </summary>
    public LedgerIssueType IssueType { get; set; }

    /// <summary>
    /// Description of the issue.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Types of issues that can be detected in ledger verification.
/// </summary>
public enum LedgerIssueType
{
    /// <summary>
    /// Gap detected in sequence numbers.
    /// </summary>
    SequenceGap,

    /// <summary>
    /// Hash chain is broken (hash mismatch).
    /// </summary>
    HashMismatch,

    /// <summary>
    /// Duplicate sequence number found.
    /// </summary>
    DuplicateSequence
}