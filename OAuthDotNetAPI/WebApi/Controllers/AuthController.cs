using Application.DTOs.Auth;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace OAuthDotNetAPI.Controllers;

[Microsoft.AspNetCore.Components.Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService, ILogger logger) : BaseAppController(logger)
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(UserLoginDto userLoginDto) => await ResolveAsync(() =>
        authService.Login(userLoginDto.Username, userLoginDto.Password,
            HttpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString() ?? ""));
    
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(UserLogoutDto request) =>
        await ResolveAsync(() => authService.Logout(request.Username, request.RefreshToken));
    
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(UserRefreshTokenDto request) => await ResolveAsync(() =>
        authService.Refresh(request.Username, request.RefreshToken,
            HttpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString() ?? ""));
    
    [HttpPost("ResetPassword/{emailAddress}")]
    public async Task<IActionResult> RequestResetPassword(string emailAddress) => await ResolveAsync(() =>
        authService.RequestPasswordReset(emailAddress,
            HttpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString() ?? ""));

    [HttpPost("ResetUserPassword")]
    public async Task<IActionResult> ApplyResetPassword(PasswordResetSubmissionDto token) => 
        await ResolveAsync(() => authService.ApplyPasswordReset(
                token, 
                HttpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString() ?? ""
            )
        );

    [HttpPost("ForcePasswordReset")]
    [Authorize]
    public async Task<IActionResult> ForceResetPassword([FromBody] string newPassword) =>
        await ResolveAsync(() => authService.ForcePasswordReset(User, newPassword));
}