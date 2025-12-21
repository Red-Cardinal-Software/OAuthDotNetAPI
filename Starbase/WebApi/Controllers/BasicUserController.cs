using Application.DTOs.Users;
using Application.Interfaces.Services;
using Application.Security;
using Asp.Versioning;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Starbase.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize]
[RequireActiveUser]
public class BasicUserController(IAppUserService appUserService, ILogger<BasicUserController> logger) : BaseAppController(logger)
{
    [HttpGet("GetAllUsers"), RequirePrivilege(PredefinedPrivileges.UserManagement.ViewBasic)]
    [ProducesResponseType(typeof(List<BasicAppUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBasicUsers() =>
        await ResolveAsync(() => appUserService.GetBasicUsersAsync(User));
}
