using Domain.Entities.Identity;
using TestUtils.Utilities;

namespace TestUtils.EntityBuilders;

public class RoleBuilder
{
    private string _name = TestConstants.Roles.DefaultName;
    private Guid? _organizationId = null;
    private readonly List<Privilege> _privileges = [];

    public static RoleBuilder New() => new();

    public RoleBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public RoleBuilder WithOrganizationId(Guid orgId)
    {
        _organizationId = orgId;
        return this;
    }

    public RoleBuilder WithPrivilege(Privilege privilege)
    {
        _privileges.Add(privilege);
        return this;
    }

    public RoleBuilder WithPrivileges(IEnumerable<Privilege> privileges)
    {
        _privileges.AddRange(privileges);
        return this;
    }

    public Role Build()
    {
        var role = new Role(_name, _organizationId);
        foreach (var privilege in _privileges)
        {
            role.AddPrivilege(privilege);
        }

        return role;
    }
}