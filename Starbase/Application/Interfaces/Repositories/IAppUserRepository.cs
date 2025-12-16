using Domain.Entities.Identity;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Interface for accessing and managing AppUser-related data.
/// </summary>
public interface IAppUserRepository
{
    /// <summary>
    /// Gets all active users for a specific organization.
    /// </summary>
    Task<List<AppUser>> GetUsersForOrganizationAsync(Guid organizationId);

    /// <summary>
    /// Gets all users for a specific organization, including inactive ones.
    /// </summary>
    Task<List<AppUser>> GetUsersForOrganizationWithInactiveAsync(Guid organizationId);

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    Task<AppUser?> GetUserByIdAsync(Guid id);

    /// <summary>
    /// Gets multiple users by their IDs.
    /// </summary>
    Task<List<AppUser>> GetUsersByIdAsync(IList<Guid> ids);

    /// <summary>
    /// Checks if a user with the given username exists in the specified organization.
    /// </summary>
    Task<bool> DoesUserExistForOrgAsync(string username, Guid organizationId);

    /// <summary>
    /// Checks if a user ID exists in the specified organization.
    /// </summary>
    Task<bool> DoesUserExistForOrgAsync(Guid appUserId, Guid organizationId);

    /// <summary>
    /// Gets a user by their email.
    /// </summary>
    Task<AppUser?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Checks if a user exists with the specified email.
    /// </summary>
    Task<bool> DoesUserExistWithEmailAsync(string email);

    public Task<AppUser> CreateUserAsync(AppUser user);

    /// <summary>
    /// Checks if a user exists by username.
    /// </summary>
    Task<bool> UserExistsAsync(string username);

    /// <summary>
    /// Retrieves a user by their username.
    /// </summary>
    Task<AppUser?> GetUserByUsernameAsync(string username);

    /// <summary>
    /// Gets the total number of users in the system.
    /// </summary>
    Task<int> GetTotalUserCountAsync();

    /// <summary>
    /// Gets the total number of users in a specific organization.
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    Task<int> GetTotalUserCountForOrganizationAsync(Guid organizationId);
}
