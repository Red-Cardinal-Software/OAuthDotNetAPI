using Domain.Entities.Identity;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Provides methods for accessing and managing roles within the application.
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Asynchronously retrieves a list of all roles.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a read-only list of all roles.
    /// </returns>
    public Task<IReadOnlyList<Role>> GetRolesAsync();

    /// <summary>
    /// Asynchronously retrieves a list of roles by their unique identifiers.
    /// </summary>
    /// <param name="ids">A list of unique identifiers representing the roles to be retrieved.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a list of roles corresponding to the provided identifiers.
    /// </returns>
    public Task<IReadOnlyList<Role>> GetRolesByIdsAsync(List<Guid> ids);

    /// <summary>
    /// Gets a role by ID.
    /// </summary>
    Task<Role?> GetRoleAsync(Guid roleId);
}
