using Domain.Entities.Identity;
using Domain.Exceptions;
using FluentAssertions;
using TestUtils.EntityBuilders;
using Xunit;

namespace Domain.Tests.Entities;

public class OrganizationTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithValidName()
    {
        var org = new Organization("Acme Inc");
        org.Name.Should().Be("Acme Inc");
        org.Active.Should().BeTrue();
        org.Users.Should().BeEmpty();
        org.Roles.Should().BeEmpty();
        org.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_ShouldThrow_WhenNameIsInvalid(string? name)
    {
        var act = () => new Organization(name!);
        act.Should().Throw<InvalidOrganizationNameException>();
    }

    [Fact]
    public void EFConstructor_ShouldInitializeEmptyCollections()
    {
        var org = new Organization();
        org.Users.Should().BeEmpty();
        org.Roles.Should().BeEmpty();
    }

    [Fact]
    public void Deactivate_ShouldSetActiveFalse()
    {
        var org = new Organization("Acme");
        org.Deactivate();
        org.Active.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ShouldThrow_WhenAlreadyInactive()
    {
        var org = new Organization("Acme");
        org.Deactivate();
        var act = () => org.Deactivate();
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Activate_ShouldSetActiveTrue()
    {
        var org = new Organization("Acme");
        org.Deactivate();
        org.Activate();
        org.Active.Should().BeTrue();
    }

    [Fact]
    public void Activate_ShouldThrow_WhenAlreadyActive()
    {
        var org = new Organization("Acme");
        var act = () => org.Activate();
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void AddUser_ShouldAddUser()
    {
        var org = new Organization("Acme");
        var user = new AppUserBuilder().Build();
        org.AddUser(user);
        org.Users.Should().Contain(user);
    }

    [Fact]
    public void AddUser_ShouldThrow_WhenUserIsNull()
    {
        var org = new Organization("Acme");
        var act = () => org.AddUser(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddUser_ShouldThrow_WhenUserAlreadyExists()
    {
        var org = new Organization("Acme");
        var user = new AppUserBuilder().Build();
        org.AddUser(user);
        var act = () => org.AddUser(user);
        act.Should().Throw<DuplicateUserException>();
    }

    [Fact]
    public void RemoveUser_ShouldRemoveUser()
    {
        var org = new Organization("Acme");
        var user = new AppUserBuilder().Build();
        org.AddUser(user);
        org.RemoveUser(user);
        org.Users.Should().NotContain(user);
    }

    [Fact]
    public void RemoveUser_ShouldThrow_WhenUserIsNull()
    {
        var org = new Organization("Acme");
        var act = () => org.RemoveUser(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveUser_ShouldThrow_WhenUserIsNotMember()
    {
        var org = new Organization("Acme");
        var user = new AppUserBuilder().Build();
        var act = () => org.RemoveUser(user);
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Rename_ShouldUpdateName()
    {
        var org = new Organization("Old");
        org.Rename("New");
        org.Name.Should().Be("New");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_ShouldThrow_WhenNameInvalid(string? name)
    {
        var org = new Organization("Old");
        var act = () => org.Rename(name!);
        act.Should().Throw<InvalidOrganizationNameException>();
    }
}
