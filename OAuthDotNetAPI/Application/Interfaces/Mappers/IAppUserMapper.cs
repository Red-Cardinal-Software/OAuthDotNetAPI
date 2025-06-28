using Application.DTOs.Users;
using Domain.Entities.Identity;

namespace Application.Interfaces.Mappers;

/// <summary>
/// Defines a contract for mapping operations related to the AppUser entity.
/// Provides methods to transform AppUser objects into various DTO representations
/// and to map DTO objects back into domain objects for creation or update operations.
/// </summary>
public interface IAppUserMapper
{
    /// <summary>
    /// Maps an <see cref="AppUser"/> domain entity to an <see cref="AppUserDto"/>.
    /// </summary>
    /// <param name="user">The <see cref="AppUser"/> entity to be mapped.</param>
    /// <returns>An <see cref="AppUserDto"/> representation of the provided <see cref="AppUser"/> entity.</returns>
    AppUserDto ToDto(AppUser user);

    /// <summary>
    /// Maps an <see cref="AppUser"/> domain entity to a <see cref="BasicAppUserDto"/>.
    /// </summary>
    /// <param name="user">The <see cref="AppUser"/> entity to be mapped.</param>
    /// <returns>A <see cref="BasicAppUserDto"/> representation of the provided <see cref="AppUser"/> entity.</returns>
    BasicAppUserDto ToBasicDto(AppUser user);

    /// <summary>
    /// Maps a <see cref="CreateNewUserDto"/> and an organization identifier into an <see cref="AppUser"/> entity for creation.
    /// </summary>
    /// <param name="userDto">The <see cref="CreateNewUserDto"/> containing user details to be mapped.</param>
    /// <param name="organizationId">The unique identifier of the organization to associate the new user with.</param>
    /// <returns>An <see cref="AppUser"/> entity ready for creation.</returns>
    Task<AppUser> MapForCreate(CreateNewUserDto userDto, Guid organizationId);

    /// <summary>
    /// Maps an existing <see cref="AppUser"/> entity for update based on the provided <see cref="AppUserDto"/>.
    /// </summary>
    /// <param name="appUser">The existing <see cref="AppUser"/> entity to be updated.</param>
    /// <param name="userDto">The <see cref="AppUserDto"/> containing updated data for mapping.</param>
    /// <returns>An updated <see cref="AppUser"/> entity with the changes from the provided <see cref="AppUserDto"/>.</returns>
    Task<AppUser> MapForUpdate(AppUser appUser, AppUserDto userDto);
}