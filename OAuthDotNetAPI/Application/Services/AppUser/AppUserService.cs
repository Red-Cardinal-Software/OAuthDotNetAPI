using System.Security.Claims;
using Application.Common.Constants;
using Application.Common.Factories;
using Application.Common.Services;
using Application.Common.Utilities;
using Application.DTOs.Users;
using Application.Interfaces.Mappers;
using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Logging;
using Application.Models;
using FluentValidation;

namespace Application.Services.AppUser;

/// <summary>
/// Provides services related to application users. This includes functionality for
/// administrative tasks such as retrieving users, deactivating users, adding new users,
/// and updating user information, as well as retrieving basic user information.
/// </summary>
public class AppUserService(
    IAppUserRepository appUserRepository,
    IUnitOfWork unitOfWork,
    IAppUserMapper appUserMapper,
    IValidator<string> passwordValidator,
    LogContextHelper<AppUserService> logger
    )
    : BaseAppService(unitOfWork), IAppUserService
{
    public async Task<ServiceResponse<List<AppUserDto>>> AdminGetUsersAsync(ClaimsPrincipal user)
    {
        var users = await appUserRepository.GetUsersForOrganizationAsync(RoleUtility.GetOrgIdFromClaims(user));

        var usersDto = users.Select(appUserMapper.ToDto).ToList();
        
        logger.InfoWithContext(
            user,
            new StructuredLogBuilder()
                .SetAction(AppUserActions.GetUsers)
                .SetStatus(LogStatuses.Success)
                .SetTarget(AppUserTargets.Org(RoleUtility.GetOrgIdFromClaims(user)))
                .SetEntity(nameof(AppUser))
                .SetDetail($"Returned {usersDto.Count} users")
            );

        return ServiceResponseFactory.Success(usersDto);
    }

    public async Task<ServiceResponse<bool>> AdminDeactivateUserAsync(ClaimsPrincipal user, Guid id) =>
        await RunWithCommitAsync(async () =>
    {
        var userToDeactivate = await appUserRepository.GetUserByIdAsync(id);
        var requestingUserOrg = RoleUtility.GetOrgIdFromClaims(user);

        if (userToDeactivate is null)
        {
            logger.WarningWithContext(
                user,
                new StructuredLogBuilder()
                    .SetAction(AppUserActions.DeactivateUser)
                    .SetStatus(LogStatuses.Failure)
                    .SetTarget(AppUserTargets.User(id))
                    .SetEntity(nameof(AppUser))
                    .SetDetail("No user found for the specified ID.")
                );
            return ServiceResponseFactory.Error<bool>(ServiceResponseConstants.UserNotFound);
        }

        if (userToDeactivate.OrganizationId != requestingUserOrg)
        {
            logger.CriticalWithContext(
                user,
                new StructuredLogBuilder()
                    .SetType(LogTypes.Security.Threat)
                    .SetAction(AppUserActions.DeactivateUser)
                    .SetStatus(LogStatuses.Failure)
                    .SetTarget(AppUserTargets.UserInOrg(userToDeactivate.Id, userToDeactivate.OrganizationId))
                    .SetEntity(nameof(AppUser))
                    .SetDetail("Unauthorized attempt to modify user in another organization")
                );
            return ServiceResponseFactory.Error<bool>(ServiceResponseConstants.UserUnauthorized);
        }

        userToDeactivate.Deactivate();
        
        logger.InfoWithContext(
            user,
            new StructuredLogBuilder()
                .SetAction(AppUserActions.DeactivateUser)
                .SetStatus(LogStatuses.Success)
                .SetTarget(AppUserTargets.UserInOrg(id, requestingUserOrg))
                .SetEntity(nameof(AppUser))
        );

        return ServiceResponseFactory.Success(true);
    });

    public async Task<ServiceResponse<AppUserDto>> AdminAddNewUserAsync(ClaimsPrincipal user, CreateNewUserDto newUser) =>
        await RunWithCommitAsync(async () =>
        {
        var validPasswordResult = await passwordValidator.ValidateAsync(newUser.Password);

        if (!validPasswordResult.IsValid)
        {
            logger.WarningWithContext(user, new StructuredLogBuilder()
                .SetAction(AppUserActions.AddUser)
                .SetStatus(LogStatuses.Failure)
                .SetTarget(AppUserTargets.Org(RoleUtility.GetOrgIdFromClaims(user)))
                .SetEntity(nameof(AppUser)));
            
            return ServiceResponseFactory.Error<AppUserDto>(string.Join(", ", validPasswordResult.Errors.Select(e => e.ErrorMessage)));
        }
        
        var requestingOrgId = RoleUtility.GetOrgIdFromClaims(user);
        var newUserEntity = await appUserMapper.MapForCreate(newUser, requestingOrgId);

        var newUserSavedEntity = await appUserRepository.CreateUserAsync(newUserEntity);
        
        var newUserDto = appUserMapper.ToDto(newUserSavedEntity);
        
        logger.InfoWithContext(
            user,
            new StructuredLogBuilder()
                .SetAction(AppUserActions.AddUser)
                .SetTarget(AppUserTargets.Org(requestingOrgId))
                .SetEntity(nameof(AppUser))
            );

        return ServiceResponseFactory.Success(newUserDto);
    });

    public async Task<ServiceResponse<AppUserDto>> UpdateUserAsync(ClaimsPrincipal user, AppUserDto appUserDto) => await RunWithCommitAsync(async () =>
    {
        var requestingOrgId = RoleUtility.GetOrgIdFromClaims(user);
        const string action = AppUserActions.UpdateUser;
        var userToUpdate = await appUserRepository.GetUserByIdAsync(appUserDto.Id);

        if (userToUpdate is null)
        {
            logger.WarningWithContext(
                user,
                new StructuredLogBuilder()
                    .SetAction(action)
                    .SetStatus(LogStatuses.Failure)
                    .SetTarget(AppUserTargets.UserInOrg(appUserDto.Id, requestingOrgId))
                    .SetEntity(nameof(AppUser))
                    .SetDetail("No user found for the specified ID.")
                );
            return ServiceResponseFactory.Error<AppUserDto>(ServiceResponseConstants.UserNotFound);
        }

        if (requestingOrgId != userToUpdate.OrganizationId)
        {
            logger.CriticalWithContext(
                user,
                new StructuredLogBuilder()
                    .SetType(LogTypes.Security.Threat)
                    .SetAction(action)
                    .SetStatus(LogStatuses.Failure)
                    .SetTarget(AppUserTargets.UserInOrg(userToUpdate.Id, userToUpdate.OrganizationId))
                    .SetEntity(nameof(AppUser))
                    .SetDetail("Unauthorized attempt to modify user in another organization")
                );
            return ServiceResponseFactory.Error<AppUserDto>(ServiceResponseConstants.UserUnauthorized);
        }
            
        userToUpdate = await appUserMapper.MapForUpdate(userToUpdate, appUserDto);

        var updatedDto = appUserMapper.ToDto(userToUpdate);

        return ServiceResponseFactory.Success(updatedDto);
    });

    public async Task<ServiceResponse<List<BasicAppUserDto>>> GetBasicUsersAsync(ClaimsPrincipal user)
    {
        var users = await appUserRepository.GetUsersForOrganizationAsync(RoleUtility.GetOrgIdFromClaims(user));
        var basicUsersDto = users.Select(appUserMapper.ToBasicDto).ToList();
        
        logger.InfoWithContext(user,
            new StructuredLogBuilder()
                .SetAction(AppUserActions.GetUsers)
                .SetStatus(LogStatuses.Success)
                .SetTarget(AppUserTargets.Org(RoleUtility.GetOrgIdFromClaims(user)))
                .SetEntity(nameof(AppUser))
                .SetDetail($"Returned {basicUsersDto.Count} users")
            );

        return ServiceResponseFactory.Success(basicUsersDto);
    }
}