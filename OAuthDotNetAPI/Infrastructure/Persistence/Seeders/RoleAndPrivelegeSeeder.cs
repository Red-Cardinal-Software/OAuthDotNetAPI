using Domain.Authorization;
using Domain.Constants;
using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seeders;

[DbDataSeeder]
public class RoleAndPrivilegeSeeder : IEntitySeeder
{
    public void PerformSeeding(DbContext dbContext)
    {
        PerformSeedingAsync(dbContext).Wait();
    }

    public async Task PerformSeedingAsync(DbContext dbContext)
{
    var privilegeSet = dbContext.Set<Privilege>();
    var roleSet = dbContext.Set<Role>();

    // Seed Privileges
    var privilegesByName = new Dictionary<string, Privilege>();
    foreach (var def in PrivilegeDefinitions.All)
    {
        var privilege = await privilegeSet.FirstOrDefaultAsync(p => p.Name == def.Name);
        if (privilege is null)
        {
            privilege = new Privilege(def.Name, def.Description, def.IsSystemDefault, def.IsAdminDefault, def.IsUserDefault);
            await privilegeSet.AddAsync(privilege);
        }

        privilegesByName[def.Name] = privilege;
    }

    await dbContext.SaveChangesAsync(); // Save privileges first

    var superAdmin = await EnsureRoleExists(PredefinedRoles.SuperAdmin);
    var admin = await EnsureRoleExists(PredefinedRoles.Admin);
    var user = await EnsureRoleExists(PredefinedRoles.User);

    // Only assign privileges if they aren't assigned yet (idempotency)
    var superAdminPrivs = new[]
    {
        PredefinedPrivileges.SystemAdministration.ManageTenants,
        PredefinedPrivileges.SystemAdministration.Secrets,
        PredefinedPrivileges.SystemAdministration.SeedingExecute,
        PredefinedPrivileges.SystemAdministration.Metrics
    };

    foreach (var priv in superAdminPrivs)
    {
        if (!superAdmin.Privileges.Contains(privilegesByName[priv]))
        {
            superAdmin.AddPrivilege(privilegesByName[priv]);
        }
    }
    
    foreach (var def in PrivilegeDefinitions.All.Where(p => p.IsAdminDefault))
    {
        if (!admin.Privileges.Contains(privilegesByName[def.Name]))
        {
            admin.AddPrivilege(privilegesByName[def.Name]);
        }
    }
    
    foreach (var def in PrivilegeDefinitions.All.Where(p => p.IsUserDefault))
    {
        if (!user.Privileges.Contains(privilegesByName[def.Name]))
        {
            user.AddPrivilege(privilegesByName[def.Name]);
        }
    }
    
    return;

    // Helper for assigning
    async Task<Role> EnsureRoleExists(string roleName)
    {
        var role = await roleSet.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role is null)
        {
            role = new Role(roleName);
            await roleSet.AddAsync(role);
        }

        return role;
    }
}
}