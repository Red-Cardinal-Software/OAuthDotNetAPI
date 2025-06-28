using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Common.Constants;
using Application.Common.Factories;
using Application.Common.Services;
using Application.Common.Utilities;
using Application.DTOs.Auth;
using Application.DTOs.Jwt;
using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Application.Interfaces.Security;
using Application.Interfaces.Services;
using Application.Logging;
using Application.Models;
using Domain.Entities.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services.Auth;

/// <summary>
/// Handles authentication workflows such as login, logout, password reset, etc.  It also handles the JWT token generation and validation. 
/// </summary>
public class AuthService(
    IAppUserRepository appUserRepository,
    IPasswordResetEmailService passwordResetEmailService,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IPasswordResetService passwordResetService,
    IRefreshTokenRepository refreshTokenRepository,
    IRoleRepository roleRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    LogContextHelper<AuthService> logger,
    IConfiguration configuration)
    : BaseAppService(unitOfWork), IAuthService
{
    
    /// <summary>
    /// Performs login flow for a user
    /// </summary>
    /// <param name="username">User supplied username</param>
    /// <param name="password">User supplied password</param>
    /// <param name="ipAddress">The IP address the user is requesting login from</param>
    /// <returns><see cref="JwtResponseDto"/> with token and refresh token to issue to the user</returns>
    public async Task<ServiceResponse<JwtResponseDto>> Login(string username, string password, string ipAddress) => await RunWithCommitAsync(async () =>
    {
        if (!await UserExists(username))
        {
            logger.Critical(new StructuredLogBuilder()
                .SetAction(AuthActions.Login)
                .SetStatus(LogStatuses.Failure)
                .SetTarget(AuthTargets.User(username))
            );
            return ServiceResponseFactory.Error<JwtResponseDto>(
                ServiceResponseConstants.UsernameOrPasswordIncorrect);
        }

        var user = await appUserRepository.GetUserByUsernameAsync(username);

        if (user is null)
        {
            logger.Critical(new StructuredLogBuilder()
                .SetAction(AuthActions.Login)
                .SetStatus(LogStatuses.Failure)
                .SetTarget(AuthTargets.User(username))
                .SetDetail(ServiceResponseConstants.AppUserNotFound));
            return ServiceResponseFactory.Error<JwtResponseDto>(
                ServiceResponseConstants.UsernameOrPasswordIncorrect);
        }

        if (string.IsNullOrWhiteSpace(user.Password) || !passwordHasher.Verify(password, user.Password))
        {
            logger.Critical(new StructuredLogBuilder()
                .SetAction(AuthActions.Login)
                .SetStatus(LogStatuses.Failure)
                .SetTarget(AuthTargets.User(username))
                .SetEntity(nameof(Domain.Entities.Identity.AppUser))
                .SetDetail(ServiceResponseConstants.UsernameOrPasswordIncorrect));
            return ServiceResponseFactory.Error<JwtResponseDto>(
                ServiceResponseConstants.UsernameOrPasswordIncorrect);
        }
        
        user.LoggedIn();

        var refreshTokenExpirationTime = GetRefreshTokenExpirationTime(configuration);

        var refreshTokenEntity = new RefreshToken(user, DateTime.UtcNow.AddHours(refreshTokenExpirationTime), ipAddress);
        await refreshTokenRepository.SaveRefreshTokenAsync(refreshTokenEntity);
        
        logger.Info(new StructuredLogBuilder()
            .SetAction(AuthActions.Login)
            .SetStatus(LogStatuses.Success)
            .SetTarget(AuthTargets.User(username))
            .SetEntity(nameof(Domain.Entities.Identity.AppUser)));

        return ServiceResponseFactory.Success(new JwtResponseDto
        {
            Token = await CreateToken(user),
            RefreshToken = refreshTokenEntity.Id.ToString(),
            ForceReset = user.ForceResetPassword
        });
    });

    /// <summary>
    /// Retrieves a new JWT Token using a refresh token
    /// </summary>
    /// <param name="username">Username of the user</param>
    /// <param name="token">The refresh token</param>
    /// <param name="ipAddress">IP Address of the user requesting</param>
    /// <returns>New <see cref="JwtResponseDto"/> with token and refresh token to issue to the user</returns>
    public async Task<ServiceResponse<JwtResponseDto>> Refresh(string username, string token, string ipAddress) => await RunWithCommitAsync(async () =>
    {
        if (!await appUserRepository.UserExistsAsync(username))
        {
            logger.Info(new StructuredLogBuilder()
                .SetType(LogTypes.Security.Alert)
                .SetAction(AuthActions.RefreshJwtToken)
                .SetStatus(LogStatuses.Failure)
                .SetTarget(AuthTargets.User(username))
                .SetEntity(nameof(Domain.Entities.Identity.AppUser))
                .SetDetail(ServiceResponseConstants.UserNotFound));
            return ServiceResponseFactory.Error<JwtResponseDto>(ServiceResponseConstants.UserUnauthorized);
        }

        var user = await appUserRepository.GetUserByUsernameAsync(username);

        if (user is null)
        {
            logger.Info(new StructuredLogBuilder()
                .SetType(LogTypes.Security.Alert)
                .SetAction(AuthActions.RefreshJwtToken)
                .SetStatus(LogStatuses.Failure)
                .SetTarget(AuthTargets.User(username))
                .SetEntity(nameof(Domain.Entities.Identity.AppUser))
                .SetDetail(ServiceResponseConstants.UserNotFound));
            return ServiceResponseFactory.Error<JwtResponseDto>(ServiceResponseConstants.UserUnauthorized);
        }
    
        var thisToken = await refreshTokenRepository.GetRefreshTokenAsync(Guid.Parse(token), user.Id);

        if (thisToken is null)
        {
            logger.Info(new StructuredLogBuilder()
                .SetType(LogTypes.Security.Alert)
                .SetAction(AuthActions.RefreshJwtToken)
                .SetStatus(LogStatuses.Failure)
                .SetTarget(AuthTargets.User(username))
                .SetEntity(nameof(RefreshToken))
                .SetDetail(ServiceResponseConstants.TokenNotFound));
            return ServiceResponseFactory.Error<JwtResponseDto>(ServiceResponseConstants.UserUnauthorized);
        }

        if (!string.IsNullOrWhiteSpace(thisToken.ReplacedBy))
        {
            logger.Info(new StructuredLogBuilder()
                .SetType(LogTypes.Security.Threat)
                .SetAction(AuthActions.RefreshJwtToken)
                .SetStatus(LogStatuses.Failure)
                .SetTarget(AuthTargets.User(username))
                .SetEntity(nameof(RefreshToken))
                .SetDetail(ServiceResponseConstants.RefreshTokenAlreadyClaimed));
            await refreshTokenRepository.RevokeRefreshTokenFamilyAsync(thisToken.TokenFamily);
            return ServiceResponseFactory.Error<JwtResponseDto>(ServiceResponseConstants.UserUnauthorized);
        }

        if (thisToken.Expires < DateTime.UtcNow)
        {
            logger.Info(new StructuredLogBuilder()
                .SetAction(AuthActions.RefreshJwtToken)
                .SetStatus(LogStatuses.Failure)
                .SetTarget(AuthTargets.User(username))
                .SetEntity(nameof(RefreshToken))
                .SetDetail(ServiceResponseConstants.RefreshTokenExpired));
            await refreshTokenRepository.RevokeRefreshTokenFamilyAsync(thisToken.TokenFamily);
            return ServiceResponseFactory.Error<JwtResponseDto>(ServiceResponseConstants.RefreshTokenExpired);
        }

        var refreshToken = Guid.NewGuid();
        var refreshTokenEntity = new RefreshToken(user, DateTime.UtcNow.AddHours(GetRefreshTokenExpirationTime(configuration)), ipAddress, thisToken.TokenFamily);
        await refreshTokenRepository.SaveRefreshTokenAsync(refreshTokenEntity);
    
        thisToken.MarkReplaced(refreshToken.ToString());

        if (!thisToken.IsValid())
        {
            logger.Info(new StructuredLogBuilder()
                .SetAction(AuthActions.RefreshJwtToken)
                .SetStatus(LogStatuses.Success)
                .SetTarget(AuthTargets.User(username))
                .SetEntity(nameof(RefreshToken)));

            return ServiceResponseFactory.Success(new JwtResponseDto
            {
                RefreshToken = refreshToken.ToString(),
                Token = await CreateToken(user)
            });
        }
        
        logger.Info(new StructuredLogBuilder()
            .SetAction(AuthActions.RefreshJwtToken)
            .SetStatus(LogStatuses.Failure)
            .SetTarget(AuthTargets.User(username))
            .SetEntity(nameof(RefreshToken))
            .SetDetail(ServiceResponseConstants.UnableToGenerateRefreshToken));
        return ServiceResponseFactory.Error<JwtResponseDto>(ServiceResponseConstants.UnableToGenerateRefreshToken);
    });

    /// <summary>
    /// Performs logout flow for a user
    /// </summary>
    /// <param name="username">Username of the requesting user</param>
    /// <param name="refreshToken">The current refresh token</param>
    /// <returns>Whether logout was successfully completed or not</returns>
    public async Task<ServiceResponse<bool>> Logout(string username, string refreshToken) => await RunWithCommitAsync(async () =>
    {
        var user = await appUserRepository.GetUserByUsernameAsync(username);

        if (user is null)
        {
            logger.Info(new StructuredLogBuilder()
                .SetType(LogTypes.Security.Threat)
                .SetAction(AuthActions.Logout)
                .SetStatus(LogStatuses.Failure)
                .SetTarget(AuthTargets.User(username))
                .SetEntity(nameof(Domain.Entities.Identity.AppUser))
                .SetDetail(ServiceResponseConstants.UserNotFound));
            return ServiceResponseFactory.Error<bool>(ServiceResponseConstants.UserUnauthorized);
        }

        var thisRefreshToken = await refreshTokenRepository.GetRefreshTokenAsync(Guid.Parse(refreshToken), user.Id);
        if (thisRefreshToken is null)
        {
            logger.Info(new StructuredLogBuilder()
                .SetType(LogTypes.Security.Threat)
                .SetAction(AuthActions.Logout)
                .SetStatus(LogStatuses.Failure)
                .SetTarget(AuthTargets.User(username))
                .SetEntity(nameof(RefreshToken))
                .SetDetail(ServiceResponseConstants.TokenNotFound));
            return ServiceResponseFactory.Error<bool>(ServiceResponseConstants.UserUnauthorized);
        }
        
        logger.Info(new StructuredLogBuilder()
            .SetAction(AuthActions.Logout)
            .SetStatus(LogStatuses.Success)
            .SetTarget(AuthTargets.User(username))
            .SetEntity(nameof(Domain.Entities.Identity.AppUser))
            .SetDetail(ServiceResponseConstants.UserLoggedOut));
        return ServiceResponseFactory.Success(await refreshTokenRepository.RevokeRefreshTokenFamilyAsync(thisRefreshToken.TokenFamily));
    });

    /// <summary>
    /// Workflow when a user submits a request to change their password.  It generates an email to the user's email address
    /// To avoid giving too much information to a potential attacker, this method will return true whether the email was sent successfully or not.
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="ipAddress">The IP address of the requestor</param>
    /// <returns>Whether the operation was successful or not, generally only if there's a database error will it return false</returns>
    public async Task<ServiceResponse<bool>> RequestPasswordReset(string email, string ipAddress) => await RunWithCommitAsync(async () =>
    {
        var user = await appUserRepository.GetUserByEmailAsync(email);

        if (user is null)
        {
            logger.Info(new StructuredLogBuilder()
                .SetType(LogTypes.Security.Alert)
                .SetAction(AuthActions.RequestPasswordReset)
                .SetStatus(LogStatuses.Failure)
                .SetTarget(AuthTargets.User(email))
                .SetEntity(nameof(Domain.Entities.Identity.AppUser))
                .SetDetail(ServiceResponseConstants.UserNotFound));
            // Do not give too much information to an attacker that is trying to probe for valid usernames
            return ServiceResponseFactory.Success(true, ServiceResponseConstants.EmailPasswordResetSent);
        }

        if (!int.TryParse(configuration["PasswordResetExpirationTime"], out var passwordResetExpirationTimeHours))
        {
            passwordResetExpirationTimeHours = SystemDefaults.DefaultPasswordResetExpirationInHours;
        }

        var newPasswordResetToken =
            new PasswordResetToken(user, DateTime.Now.AddHours(passwordResetExpirationTimeHours), ipAddress);

        var tokenEntity = await passwordResetTokenRepository.CreateResetPasswordTokenAsync(newPasswordResetToken);

        await passwordResetEmailService.SendPasswordResetEmail(user, tokenEntity);

        logger.Info(new StructuredLogBuilder()
            .SetAction(AuthActions.RequestPasswordReset)
            .SetStatus(LogStatuses.Success)
            .SetTarget(AuthTargets.User(email))
            .SetEntity(nameof(Domain.Entities.Identity.AppUser))
            .SetDetail(ServiceResponseConstants.EmailPasswordResetSent));

        return ServiceResponseFactory.Success(true, ServiceResponseConstants.EmailPasswordResetSent);
    });

    /// <summary>
    /// Applies a password reset with the reset token assigned to the request
    /// </summary>
    /// <param name="token">The Full password request submission including a new password and token</param>
    /// <param name="ipAddress">IP Address of the requestor</param>
    /// <returns>Whether the password reset was successful or not</returns>
    public async Task<ServiceResponse<bool>> ApplyPasswordReset(PasswordResetSubmissionDto token, string ipAddress) =>
        await RunWithCommitAsync(async () =>
            await passwordResetService.ResetPasswordWithTokenAsync(token.Token, token.Password, ipAddress));

    /// <summary>
    /// Workflow for when a user logs in, and they have the Force Reset Password flag set.  This method will force the user to change their password
    /// </summary>
    /// <param name="user">The claims principal of the user trying to log in</param>
    /// <param name="newPassword">The supplied new password</param>
    /// <returns>Whether the operation was successful or not</returns>
    public async Task<ServiceResponse<bool>> ForcePasswordReset(ClaimsPrincipal user, string newPassword) => await RunWithCommitAsync(async () =>
    {
        var userId = RoleUtility.GetUserIdFromClaims(user);
        return await passwordResetService.ForcePasswordResetAsync(userId, newPassword);
    });

    /// <summary>
    /// Generates a new JWT token for a user.  Used when they change a setting that requires regenerating a token
    /// </summary>
    /// <param name="user">The claims principal of the user trying to log in</param>
    /// <returns>New <see cref="JwtResponseDto"/> with token and refresh token to issue to the user</returns>
    public async Task<JwtResponseDto> GenerateJwtToken(ClaimsPrincipal user)
    {
        var appUser = await appUserRepository.GetUserByUsernameAsync(RoleUtility.GetUserNameFromClaim(user));
        
        var refreshToken = Guid.NewGuid();
        
        return new JwtResponseDto
        {
            RefreshToken = refreshToken.ToString(),
            Token = await CreateToken(appUser!)
        };
    }
    
    /// <summary>
    /// General check if the user actually exists in the system
    /// </summary>
    /// <param name="username">The username of the user</param>
    /// <returns>Whether the user exists or not</returns>
    private async Task<bool> UserExists(string username)
    {
        return await appUserRepository.UserExistsAsync(username);
    }

    /// <summary>
    /// Retreives the configured refresh token expiration time
    /// </summary>
    /// <param name="configuration">Configuration to get the set hours of expiration</param>
    /// <returns>The configured set hours for expiration or the system default</returns>
    private static int GetRefreshTokenExpirationTime(IConfiguration configuration)
    {
        if (!int.TryParse(configuration["RefreshTokenExpirationTime"], out var refreshTokenExpirationTime))
        {
            refreshTokenExpirationTime = SystemDefaults.DefaultRefreshTokenExpirationTimeInHours;
        }
        
        return refreshTokenExpirationTime;
    }
    
    /// <summary>
    /// Logic to construct the JWT Token
    /// </summary>
    /// <param name="user">The App user with all properties</param>
    /// <returns>The string of the JWT token</returns>
    /// <exception cref="Exception">Thrown if there is no config for the token signature key.  It's required to exist and recommended to be unique per environment</exception>
    private async Task<string> CreateToken(Domain.Entities.Identity.AppUser user)
    {
        var claims = new List<Claim>
        {
            new (ClaimTypes.NameIdentifier, user.Id.ToString()),
            new (ClaimTypes.Name, user.Username),
            new ("Organization", user.OrganizationId.ToString())
        };
        
        // Add privileges as individual claims
        var privilegeNames = user.Roles
            .SelectMany(r => r.Privileges)
            .Select(p => p.Name)
            .Distinct();

        claims.AddRange(privilegeNames.Select(priv => new Claim("priv", priv)));

        var allRoles = await roleRepository.GetRolesAsync();
        var userInfoClaims = ClaimsUtility.BuildClaimsForUser(user, allRoles);
        claims.AddRange(userInfoClaims);

        var appSettingsToken = configuration["AppSettings-Token"];
        if (appSettingsToken is null)
        {
            throw new Exception("AppSettings Token is null");
        }

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(appSettingsToken));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        if (!int.TryParse(configuration["JwtExpirationTimeMinutes"], out var jwtExpirationTimeMinutes))
        {
            jwtExpirationTimeMinutes = SystemDefaults.DefaultJwtExpirationTimeInMinutes;
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(jwtExpirationTimeMinutes),
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}