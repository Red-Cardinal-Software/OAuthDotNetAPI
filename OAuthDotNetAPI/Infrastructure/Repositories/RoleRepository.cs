using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository for managing Role entities in the system.
/// Provides methods for retrieving role data from the database.
/// </summary>
public class RoleRepository(ICrudOperator<Role> roleCrudOperator) : IRoleRepository
{
    public async Task<IReadOnlyList<Role>> GetRolesAsync() => await GetAllWithChildren().ToListAsync();

    public async Task<IReadOnlyList<Role>> GetRolesByIdsAsync(List<Guid> ids) => await GetAllWithChildren().Where(r => ids.Contains(r.Id)).ToListAsync();

    public async Task<Role?> GetRoleAsync(Guid roleId) => await GetAllWithChildren().FirstOrDefaultAsync(r => r.Id == roleId);

    /// <summary>
    /// Retrieves all Role entities from the database along with their related child entities,
    /// including associated privileges.
    /// </summary>
    /// <returns>An <see cref="IQueryable{T}"/> of <see cref="Role"/> objects with their related child entities.</returns>
    private IQueryable<Role> GetAllWithChildren() =>
        roleCrudOperator.GetAll()
            .Include(r => r.Privileges);
}