using Application.DTOs.Audit;
using Application.Interfaces.Services;
using Application.Models;
using Application.Security;
using Asp.Versioning;
using AutoMapper;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Starbase.Controllers;

/// <summary>
/// Controller for querying and verifying the audit ledger.
/// Provides endpoints for compliance officers and security teams to access audit logs,
/// verify ledger integrity, and view archived partitions.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize]
[RequireActiveUser]
[EnableRateLimiting("api")]
public class AuditController(
    IAuditLedger auditLedger,
    IAuditArchiver auditArchiver,
    IMapper mapper,
    ILogger<AuditController> logger) : BaseAppController(logger)
{
    #region Audit Log Queries

    /// <summary>
    /// Query audit log entries with filtering and pagination.
    /// </summary>
    /// <param name="query">Query parameters including filters and pagination.</param>
    /// <returns>Paginated list of audit entries matching the criteria.</returns>
    [HttpGet("logs")]
    [RequirePrivilege(PredefinedPrivileges.Audit.View)]
    [ProducesResponseType(typeof(PagedResult<AuditEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> QueryLogs([FromQuery] AuditQueryDto query) =>
        await ResolveAsync(() => auditLedger.QueryAsync(query));

    /// <summary>
    /// Get the complete audit history for a specific entity.
    /// </summary>
    /// <param name="entityType">The type of entity (e.g., "AppUser", "Role").</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <returns>List of all audit entries for the specified entity.</returns>
    [HttpGet("entity/{entityType}/{entityId}")]
    [RequirePrivilege(PredefinedPrivileges.Audit.View)]
    [ProducesResponseType(typeof(List<AuditEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEntityHistory(string entityType, string entityId) =>
        await ResolveAsync(() => auditLedger.GetEntityHistoryAsync(entityType, entityId));

    /// <summary>
    /// Get audit activity for a specific user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <returns>List of audit entries for the specified user.</returns>
    [HttpGet("user/{userId:guid}")]
    [RequirePrivilege(PredefinedPrivileges.Audit.View)]
    [ProducesResponseType(typeof(List<AuditEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserActivity(
        Guid userId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null) =>
        await ResolveAsync(() => auditLedger.GetUserActivityAsync(userId, fromDate, toDate));

    #endregion

    #region Integrity Verification

    /// <summary>
    /// Verify the integrity of the audit ledger hash chain.
    /// Checks for sequence gaps and hash mismatches within the specified range.
    /// </summary>
    /// <param name="fromSequence">Optional starting sequence number (defaults to first entry).</param>
    /// <param name="toSequence">Optional ending sequence number (defaults to last entry).</param>
    /// <returns>Verification result including any issues found.</returns>
    [HttpGet("verify")]
    [RequirePrivilege(PredefinedPrivileges.Audit.Verify)]
    [ProducesResponseType(typeof(LedgerVerificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyIntegrity(
        [FromQuery] long? fromSequence = null,
        [FromQuery] long? toSequence = null) =>
        await ResolveAsync(() => auditLedger.VerifyIntegrityAsync(fromSequence, toSequence));

    #endregion

    #region Archive Management

    /// <summary>
    /// Get all archive manifests with optional date range filtering.
    /// </summary>
    /// <param name="fromDate">Optional start date for partition boundary filter.</param>
    /// <param name="toDate">Optional end date for partition boundary filter.</param>
    /// <returns>List of archive manifests.</returns>
    [HttpGet("archives")]
    [RequirePrivilege(PredefinedPrivileges.Audit.ViewArchives)]
    [ProducesResponseType(typeof(List<ArchiveManifestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetArchives(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null) =>
        await ResolveAsync(async () =>
        {
            var manifests = await auditArchiver.GetArchiveManifestsAsync(fromDate, toDate);
            return mapper.Map<List<ArchiveManifestDto>>(manifests);
        });

    /// <summary>
    /// Verify the integrity of an archived partition by checking the blob hash.
    /// </summary>
    /// <param name="archiveId">The archive manifest ID to verify.</param>
    /// <returns>Verification result indicating if the archive is intact.</returns>
    [HttpGet("archives/{archiveId:guid}/verify")]
    [RequirePrivilege(PredefinedPrivileges.Audit.ViewArchives)]
    [ProducesResponseType(typeof(ArchiveVerificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyArchive(Guid archiveId) =>
        await ResolveAsync(() => auditArchiver.VerifyArchiveAsync(archiveId));

    #endregion
}