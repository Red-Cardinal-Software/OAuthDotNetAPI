using Application.Common.Constants;
using Application.Common.Utilities;
using Application.DTOs.Mfa;
using Application.Interfaces.Services;
using Application.Security;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace OAuthDotNetAPI.Controllers;

/// <summary>
/// Administrative controller for multi-factor authentication metrics and management.
/// Provides system-wide and organization-scoped MFA statistics for authorized administrators.
/// </summary>
[Route("api/admin/[controller]")]
[ApiController]
[Authorize]
[RequireActiveUser]
[EnableRateLimiting("api")]
public class MfaAdminController(
    IMfaConfigurationService mfaConfigurationService,
    ILogger<MfaAdminController> logger) : BaseAppController(logger)
{
    /// <summary>
    /// Gets system-wide MFA statistics. Requires super admin privileges.
    /// </summary>
    /// <returns>System-wide MFA adoption metrics and statistics</returns>
    [HttpGet("statistics/system")]
    [RequirePrivilege(PredefinedPrivileges.SystemAdministration.Metrics)]
    [ProducesResponseType(typeof(MfaStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSystemMfaStatistics()
    {
        return await ResolveAsync(() => mfaConfigurationService.GetMfaStatisticsAsync());
    }

    /// <summary>
    /// Gets MFA statistics for a specific organization. Super admins can view any organization.
    /// </summary>
    /// <param name="organizationId">The organization ID to get statistics for</param>
    /// <returns>Organization-specific MFA adoption metrics and statistics</returns>
    [HttpGet("statistics/organization/{organizationId:guid}")]
    [RequirePrivilege(PredefinedPrivileges.SystemAdministration.Metrics)]
    [ProducesResponseType(typeof(MfaStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrganizationMfaStatistics(Guid organizationId)
    {
        return await ResolveAsync(() => mfaConfigurationService.GetMfaStatisticsAsync(organizationId));
    }

    /// <summary>
    /// Gets MFA statistics for the current user's organization. Organization admins can only view their own organization.
    /// </summary>
    /// <returns>Current organization's MFA adoption metrics and statistics</returns>
    [HttpGet("statistics/my-organization")]
    [RequirePrivilege(PredefinedPrivileges.OrganizationManagement.MfaMetrics)]
    [ProducesResponseType(typeof(MfaStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyOrganizationMfaStatistics()
    {
        return await ResolveAsync(() =>
        {
            var organizationId = RoleUtility.GetOrgIdFromClaims(User);
            return mfaConfigurationService.GetMfaStatisticsAsync(organizationId);
        });
    }

    /// <summary>
    /// Cleans up unverified MFA methods older than the specified number of hours. 
    /// Requires system administration privileges.
    /// </summary>
    /// <param name="olderThanHours">Remove unverified methods older than this many hours (default: 24)</param>
    /// <returns>Number of unverified methods that were cleaned up</returns>
    [HttpDelete("cleanup/unverified")]
    [RequirePrivilege(PredefinedPrivileges.SystemAdministration.Metrics)]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CleanupUnverifiedMethods([FromQuery] int olderThanHours = 24)
    {
        if (olderThanHours is < 1 or > 8760)
        {
            return BadRequest("olderThanHours must be between 1 and 8760 hours (1 year)");
        }

        var maxAge = TimeSpan.FromHours(olderThanHours);
        return await ResolveAsync(() => mfaConfigurationService.CleanupUnverifiedMethodsAsync(maxAge));
    }
}
