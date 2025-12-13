using Domain.Entities.Identity;
using Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Entities;

public class RoleTests
{
    private Privilege CreatePrivilege(string name = "ViewReports") =>
        new(name, "View Reports", isSystemDefault: false, isAdminDefault: false, isUserDefault: false);

    [Fact]
    public void Constructor_ShouldInitialize_WithValidName()
    {
        var orgId = Guid.NewGuid();
        var role = new Role("Manager", orgId);

        role.Id.Should().NotBe(Guid.Empty);
        role.Name.Should().Be("Manager");
        role.OrganizationId.Should().Be(orgId);
        role.Users.Should().BeEmpty();
        role.Privileges.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenNameIsInvalid(string? name)
    {
        var act = () => new Role(name!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("name");
    }

    [Fact]
    public void Rename_ShouldUpdateName()
    {
        var role = new Role("OldName");

        role.Rename("  NewName  ");

        role.Name.Should().Be("NewName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Rename_ShouldThrow_WhenNameInvalid(string? newName)
    {
        var role = new Role("ValidName");

        var act = () => role.Rename(newName!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("newName");
    }

    [Fact]
    public void AddPrivilege_ShouldAdd_WhenNotAlreadyExists()
    {
        var role = new Role("Admin");
        var privilege = CreatePrivilege();

        role.AddPrivilege(privilege);

        role.Privileges.Should().ContainSingle(p => p == privilege);
    }

    [Fact]
    public void AddPrivilege_ShouldThrow_WhenNull()
    {
        var role = new Role("Admin");

        var act = () => role.AddPrivilege(null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("privilege");
    }

    [Fact]
    public void AddPrivilege_ShouldThrow_WhenDuplicate()
    {
        var role = new Role("Admin");
        var privilege = CreatePrivilege();

        role.AddPrivilege(privilege);

        var act = () => role.AddPrivilege(privilege);

        act.Should().Throw<DuplicatePrivilegeException>()
           .WithMessage($"*{privilege.Name}*");
    }

    [Fact]
    public void RemovePrivilege_ShouldRemove_WhenExists()
    {
        var role = new Role("Admin");
        var privilege = CreatePrivilege();
        role.AddPrivilege(privilege);

        role.RemovePrivilege(privilege);

        role.Privileges.Should().BeEmpty();
    }

    [Fact]
    public void RemovePrivilege_ShouldThrow_WhenNull()
    {
        var role = new Role("Admin");

        var act = () => role.RemovePrivilege(null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("privilege");
    }

    [Fact]
    public void RemovePrivilege_ShouldThrow_WhenNotFound()
    {
        var role = new Role("Admin");
        var privilege = CreatePrivilege();

        var act = () => role.RemovePrivilege(privilege);

        act.Should().Throw<InvalidStateTransitionException>()
           .WithMessage("*not set*");
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenNamesMatch_IgnoringCase()
    {
        var role1 = new Role("manager");
        var role2 = new Role("MANAGER");

        role1.Equals(role2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenNamesDiffer()
    {
        var role1 = new Role("Manager");
        var role2 = new Role("Worker");

        role1.Equals(role2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldBeSame_ForEqualNames()
    {
        var role1 = new Role("Manager");
        var role2 = new Role("manager");

        role1.GetHashCode().Should().Be(role2.GetHashCode());
    }
}
