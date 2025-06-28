using Domain.Entities.Identity;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Entities;

public class PrivilegeTests
{
    [Fact]
    public void Constructor_ShouldInitialize_WithValidData()
    {
        var privilege = new Privilege("EditUsers", "Can edit users", true, true, false);

        privilege.Id.Should().NotBe(Guid.Empty);
        privilege.Name.Should().Be("EditUsers");
        privilege.Description.Should().Be("Can edit users");
        privilege.IsSystemDefault.Should().BeTrue();
        privilege.IsAdminDefault.Should().BeTrue();
        privilege.IsUserDefault.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenNameIsNull()
    {
        var act = () => new Privilege(null!, "desc", false, false, false);
        act.Should().Throw<ArgumentNullException>().WithParameterName("name");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenDescriptionIsNull()
    {
        var act = () => new Privilege("Name", null!, false, false, false);
        act.Should().Throw<ArgumentNullException>().WithParameterName("description");
    }

    [Fact]
    public void Rename_ShouldChangeName_WhenValid()
    {
        var privilege = new Privilege("OldName", "desc", false, false, false);

        privilege.Rename("NewName");

        privilege.Name.Should().Be("NewName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_ShouldThrow_WhenNewNameIsInvalid(string? newName)
    {
        var privilege = new Privilege("Valid", "desc", false, false, false);

        var act = () => privilege.Rename(newName!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("newName");
    }

    [Fact]
    public void Equals_ShouldReturnTrue_ForSameName_IgnoringCase()
    {
        var p1 = new Privilege("ManageStuff", "desc", false, false, false);
        var p2 = new Privilege("managestuff", "other", true, true, true);

        p1.Equals(p2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_ForDifferentName()
    {
        var p1 = new Privilege("A", "desc", false, false, false);
        var p2 = new Privilege("B", "desc", false, false, false);

        p1.Equals(p2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenOtherIsNull()
    {
        var p1 = new Privilege("A", "desc", false, false, false);

        p1.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldBeCaseInsensitive()
    {
        var p1 = new Privilege("SomeAction", "desc", false, false, false);
        var p2 = new Privilege("someaction", "desc", false, false, false);

        p1.GetHashCode().Should().Be(p2.GetHashCode());
    }
}