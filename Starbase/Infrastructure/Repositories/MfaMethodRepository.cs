using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Domain.Entities.Identity;
using Domain.Entities.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing MfaMethod entities.
/// Provides efficient data access methods for multi-factor authentication configuration
/// with optimized queries for common MFA operations and security checks.
/// </summary>
public class MfaMethodRepository(ICrudOperator<MfaMethod> mfaMethodCrudOperator) : IMfaMethodRepository
{
    /// <summary>
    /// Gets an MFA method by its unique identifier.
    /// </summary>
    public async Task<MfaMethod?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await mfaMethodCrudOperator.GetAll()
            .Include(m => m.RecoveryCodes)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets all MFA methods for a specific user.
    /// </summary>
    public async Task<IReadOnlyList<MfaMethod>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var methods = await mfaMethodCrudOperator.GetAll()
            .Include(m => m.RecoveryCodes)
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.IsDefault)
            .ThenByDescending(m => m.IsEnabled)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return methods.AsReadOnly();
    }

    /// <summary>
    /// Gets all enabled MFA methods for a specific user.
    /// </summary>
    public async Task<IReadOnlyList<MfaMethod>> GetEnabledByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var methods = await mfaMethodCrudOperator.GetAll()
            .Include(m => m.RecoveryCodes)
            .Where(m => m.UserId == userId && m.IsEnabled)
            .OrderByDescending(m => m.IsDefault)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return methods.AsReadOnly();
    }

    /// <summary>
    /// Gets a specific MFA method by user ID and type.
    /// </summary>
    public async Task<MfaMethod?> GetByUserAndTypeAsync(Guid userId, MfaType type, CancellationToken cancellationToken = default)
    {
        return await mfaMethodCrudOperator.GetAll()
            .Include(m => m.RecoveryCodes)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Type == type, cancellationToken);
    }

    /// <summary>
    /// Gets the default MFA method for a user.
    /// </summary>
    public async Task<MfaMethod?> GetDefaultByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await mfaMethodCrudOperator.GetAll()
            .Include(m => m.RecoveryCodes)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.IsDefault && m.IsEnabled, cancellationToken);
    }

    /// <summary>
    /// Checks if a user has any enabled MFA methods.
    /// </summary>
    public async Task<bool> UserHasEnabledMfaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await mfaMethodCrudOperator.GetAll()
            .AnyAsync(m => m.UserId == userId && m.IsEnabled, cancellationToken);
    }

    /// <summary>
    /// Gets the count of enabled MFA methods for a user.
    /// </summary>
    public async Task<int> GetEnabledCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await mfaMethodCrudOperator.GetAll()
            .CountAsync(m => m.UserId == userId && m.IsEnabled, cancellationToken);
    }

    /// <summary>
    /// Adds a new MFA method to the repository.
    /// </summary>
    public async Task AddAsync(MfaMethod mfaMethod, CancellationToken cancellationToken = default)
    {
        await mfaMethodCrudOperator.AddAsync(mfaMethod);
    }


    /// <summary>
    /// Removes an MFA method from the repository.
    /// </summary>
    public void Remove(MfaMethod mfaMethod)
    {
        mfaMethodCrudOperator.Delete(mfaMethod);
    }

    /// <summary>
    /// Removes all default flags from a user's MFA methods.
    /// Used when setting a new default method.
    /// </summary>
    public async Task ClearDefaultFlagsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userMethods = await mfaMethodCrudOperator.GetAll()
            .Where(m => m.UserId == userId && m.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var method in userMethods)
        {
            method.RemoveDefault();
        }
    }

    /// <summary>
    /// Gets MFA methods that haven't been verified within a specific time period.
    /// Used for cleanup operations.
    /// </summary>
    public async Task<IReadOnlyList<MfaMethod>> GetUnverifiedOlderThanAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default)
    {
        var methods = await mfaMethodCrudOperator.GetAll()
            .Where(m => !m.IsEnabled && m.CreatedAt < olderThan)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return methods.AsReadOnly();
    }

    /// <summary>
    /// Gets the number of users with at least one enabled MFA method.
    /// </summary>
    public async Task<int> GetUsersWithMfaCountAsync(CancellationToken cancellationToken = default)
    {
        return await mfaMethodCrudOperator.GetAll()
            .Where(m => m.IsEnabled)
            .Select(m => m.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the count of enabled MFA methods grouped by type.
    /// </summary>
    public async Task<Dictionary<MfaType, int>> GetMethodCountByTypeAsync(CancellationToken cancellationToken = default)
    {
        return await mfaMethodCrudOperator.GetAll()
            .Where(m => m.IsEnabled)
            .GroupBy(m => m.Type)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
    }

    /// <summary>
    /// Gets the count of unverified MFA setups.
    /// </summary>
    public async Task<int> GetUnverifiedMethodCountAsync(CancellationToken cancellationToken = default)
    {
        return await mfaMethodCrudOperator.GetAll()
            .CountAsync(m => !m.IsEnabled, cancellationToken);
    }

    /// <summary>
    /// Gets the number of users with at least one enabled MFA method in a specific organization.
    /// </summary>
    public async Task<int> GetUsersWithMfaCountForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await mfaMethodCrudOperator.GetAll()
            .Include(m => m.User)
            .Where(m => m.IsEnabled && m.User.OrganizationId == organizationId)
            .Select(m => m.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the count of enabled MFA methods grouped by type for a specific organization.
    /// </summary>
    public async Task<Dictionary<MfaType, int>> GetMethodCountByTypeForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await mfaMethodCrudOperator.GetAll()
            .Include(m => m.User)
            .Where(m => m.IsEnabled && m.User.OrganizationId == organizationId)
            .GroupBy(m => m.Type)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
    }

    /// <summary>
    /// Gets the count of unverified MFA setups for a specific organization.
    /// </summary>
    public async Task<int> GetUnverifiedMethodCountForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await mfaMethodCrudOperator.GetAll()
            .Include(m => m.User)
            .CountAsync(m => !m.IsEnabled && m.User.OrganizationId == organizationId, cancellationToken);
    }
}
