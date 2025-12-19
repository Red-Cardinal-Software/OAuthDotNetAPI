using Application.DTOs.Users;
using Application.Interfaces.Services;
using Application.Security;
using Application.Validators;
using Asp.Versioning;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Starbase.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/[controller]")]
[ApiController]
[Authorize]
[RequireActiveUser]
[EnableRateLimiting("api")]
public class UserController(IAppUserService appUserService, ILogger<UserController> logger) : BaseAppController(logger)
{
    [HttpGet("GetAllUsers"), RequirePrivilege(PredefinedPrivileges.UserManagement.View)]
    [ProducesResponseType(typeof(List<AppUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUsers() => await ResolveAsync(() => appUserService.AdminGetUsersAsync(User));

    [HttpDelete("{id:guid}"), RequirePrivilege(PredefinedPrivileges.UserManagement.Deactivate)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeactivateUser(Guid id) =>
        await ResolveAsync(() => appUserService.AdminDeactivateUserAsync(User, id));

    [HttpPost, RequirePrivilege(PredefinedPrivileges.UserManagement.Create), ValidDto]
    public async Task<IActionResult> AddNewUser(CreateNewUserDto newUserDto) =>
        await ResolveAsync(() => appUserService.AdminAddNewUserAsync(User, newUserDto));

    [HttpPut, RequirePrivilege(PredefinedPrivileges.UserManagement.Update), ValidDto]
    public async Task<IActionResult> UpdateUser(AppUserDto userToUpdate) => await ResolveAsync(() => appUserService.UpdateUserAsync(User, userToUpdate));
}