using Application.DTOs.Auth;
using Application.DTOs.Users;
using Application.Interfaces.Mappers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Security;
using AutoMapper;
using Domain.Entities.Identity;

namespace Application.Mapper.Custom;

/// <summary>
/// Provides mapping functionality between application user objects and their respective DTOs.
/// Also responsible for mapping data for creating and updating users within the application.
/// </summary>
public class AppUserMapper(IMapper mapper, IPasswordHasher passwordHasher, IRoleRepository roleRepository) : IAppUserMapper
{
    public AppUserDto ToDto(AppUser user) => new()
    {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ForceResetPassword = user.ForceResetPassword,
            Active = user.Active,
            LastLoginTime = user.LastLoginTime,
            Roles = mapper.Map<List<RoleDto>>(user.Roles)
    };

    public BasicAppUserDto ToBasicDto(AppUser user) => 
    new()
    {
        Id = user.Id,
        Username = user.Username,
        FirstName = user.FirstName,
        LastName = user.LastName
    };

    public async Task<AppUser> MapForCreate(CreateNewUserDto newUserDto, Guid organizationId)
    {
        if (newUserDto.Password is null)
            throw new ArgumentNullException(nameof(newUserDto.Password));

        var hashedPassword = passwordHasher.Hash(newUserDto.Password);
        var userRoles = await roleRepository.GetRolesByIdsAsync(newUserDto.Roles.Select(x => x.Id).ToList());
        var newUser = new AppUser(newUserDto.Username, hashedPassword, newUserDto.FirstName, newUserDto.LastName, organizationId);
        foreach(var role in userRoles)
        {
            newUser.AddRole(role);
        }

        return newUser;
    }

    public async Task<AppUser> MapForUpdate(AppUser appUser, AppUserDto userDto)
    {
        if (userDto.FirstName != appUser.FirstName)
        {
            appUser.ChangeFirstName(userDto.FirstName);
        }

        if (userDto.LastName != appUser.LastName)
        {
            appUser.ChangeLastName(userDto.LastName);
        }

        var incomingRoleIds = userDto.Roles.Select(r => r.Id).ToList();
        
        var requestedRoles = await roleRepository.GetRolesByIdsAsync(incomingRoleIds);

        SyncRoles(appUser, incomingRoleIds, requestedRoles);

        return appUser;
    }

    /// <summary>
    /// Synchronizes the roles of the specified application user by removing roles that are no longer included
    /// in the incoming role identifiers and adding roles that are newly present in the requested roles list.
    /// </summary>
    /// <param name="appUser">The application user whose roles will be synchronized.</param>
    /// <param name="incomingRoleIds">A list of role identifiers representing the user's updated roles.</param>
    /// <param name="requestedRoles">A read-only list of role objects fetched based on the incoming role identifiers.</param>
    private static void SyncRoles(AppUser appUser, List<Guid> incomingRoleIds, IReadOnlyList<Role> requestedRoles)
    {
        var rolesToRemove = appUser.Roles.Where(r => !incomingRoleIds.Contains(r.Id)).ToList();
        foreach (var role in rolesToRemove)
        {
            appUser.RemoveRole(role);
        }

        var existingRoleIds = appUser.Roles.Select(r => r.Id).ToList();

        var rolesToAdd = requestedRoles.Where(r => !existingRoleIds.Contains(r.Id)).ToList();
        foreach (var role in rolesToAdd)
        {
            appUser.AddRole(role);
        }
    }
}