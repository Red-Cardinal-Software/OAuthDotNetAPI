using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository class for managing operations related to AppUser entities.
/// Provides data retrieval and manipulation functionality for AppUser objects within an organization.
/// Implements the IAppUserRepository interface.
/// </summary>
public class AppUserRepository(
    ICrudOperator<AppUser> userCrudOperator)
    : IAppUserRepository
{
    public Task<List<AppUser>> GetUsersForOrganizationAsync(Guid organizationId) =>
        GetAllUsersWithChildren()
            .Where(u => u.Active && u.OrganizationId == organizationId).ToListAsync();

    public Task<List<AppUser>> GetUsersForOrganizationWithInactiveAsync(Guid organizationId) =>
        GetAllUsersWithChildren()
            .Where(u => u.OrganizationId == organizationId)
            .OrderByDescending(u => u.Active)
            .ToListAsync();

    public Task<AppUser?> GetUserByIdAsync(Guid id) =>
        GetAllUsersWithChildren()
            .FirstOrDefaultAsync(x => x.Id == id);

    public Task<List<AppUser>> GetUsersByIdAsync(IList<Guid> ids) =>
        GetAllUsersWithChildren()
            .Where(u => ids.Contains(u.Id))
            .ToListAsync();

    public Task<bool> DoesUserExistForOrgAsync(string username, Guid organizationId) =>
        GetAllUsersWithChildren()
            .AnyAsync(u => u.Username == username && u.OrganizationId == organizationId);

    public Task<bool> DoesUserExistForOrgAsync(Guid appUserId, Guid organizationId) =>
        GetAllUsersWithChildren()
            .AnyAsync(u => u.Id == appUserId && u.OrganizationId == organizationId);

    public Task<AppUser?> GetUserByEmailAsync(string email) =>
        GetAllUsersWithChildren()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == email.ToLower() && u.Active);

    public Task<bool> DoesUserExistWithEmailAsync(string email) =>
        GetAllUsersWithChildren()
            .AnyAsync(u => u.Username.ToLower() == email.ToLower() && u.Active);

    public async Task<AppUser> CreateUserAsync(AppUser user)
    {
        var newUser = await userCrudOperator.AddAsync(user);
        return newUser;
    }

    public async Task<bool> UserExistsAsync(string username) =>
        await userCrudOperator.GetAll().AnyAsync(u => u.Username.ToLower() == username.ToLower());


    public async Task<AppUser?> GetUserByUsernameAsync(string username) =>
        await userCrudOperator.GetAll().FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

    public async Task<int> GetTotalUserCountAsync() =>
        await userCrudOperator.GetAll().CountAsync();

    public async Task<int> GetTotalUserCountForOrganizationAsync(Guid organizationId) =>
        await userCrudOperator.GetAll()
            .CountAsync(u => u.OrganizationId == organizationId);

    /// <summary>
    /// Retrieves a queryable collection of all users, including their associated roles, privileges, and organization details.
    /// </summary>
    /// <returns>A queryable collection representing all users with their related roles, privileges, and organization included.</returns>
    private IQueryable<AppUser> GetAllUsersWithChildren() =>
        userCrudOperator.GetAll()
            .Include(u => u.Roles)
                .ThenInclude(r => r.Privileges)
            .Include(u => u.Organization);
}
