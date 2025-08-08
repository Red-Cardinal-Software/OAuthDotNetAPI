using Domain.Entities.Identity;
using FluentAssertions;
using TestUtils.EntityBuilders;
using Xunit;

namespace Domain.Tests.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void Constructor_ShouldInitializeCorrectly_WithValidData()
    {
        var user = new AppUserBuilder().Build();
        var expiration = DateTime.UtcNow.AddMinutes(10);
        var ip = "192.168.1.1";

        var token = new RefreshToken(user, expiration, ip);

        token.Id.Should().NotBe(Guid.Empty);
        token.TokenFamily.Should().NotBe(Guid.Empty);
        token.AppUserId.Should().Be(user.Id);
        token.AppUser.Should().Be(user);
        token.Expires.Should().Be(expiration);
        token.CreatedByIp.Should().Be(ip);
        token.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        token.ReplacedBy.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldUseProvidedTokenFamily()
    {
        var user = new AppUserBuilder().Build();
        var tokenFamily = Guid.NewGuid();
        var token = new RefreshToken(user, DateTime.UtcNow.AddMinutes(5), "127.0.0.1", tokenFamily);

        token.TokenFamily.Should().Be(tokenFamily);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenUserIsNull()
    {
        var act = () => new RefreshToken(null!, DateTime.UtcNow, "ip");

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("appUser");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenIpIsNull()
    {
        var user = new AppUserBuilder().Build();
        var act = () => new RefreshToken(user, DateTime.UtcNow, null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("createdByIp");
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenNotExpired()
    {
        var user = new AppUserBuilder().Build();
        var token = new RefreshToken(user, DateTime.UtcNow.AddMinutes(5), "ip");

        token.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenExpired()
    {
        var user = new AppUserBuilder().Build();
        var token = new RefreshToken(user, DateTime.UtcNow.AddMinutes(-1), "ip");

        token.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenNotExpiredAndReplaced()
    {
        var user = new AppUserBuilder().Build();
        var token = new RefreshToken(user, DateTime.UtcNow.AddMinutes(5), "ip");
        token.MarkReplaced("new-token-id");

        token.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenExpired()
    {
        var user = new AppUserBuilder().Build();
        var token = new RefreshToken(user, DateTime.UtcNow.AddMinutes(-5), "ip");
        token.MarkReplaced("replacement");

        token.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenNotReplaced()
    {
        var user = new AppUserBuilder().Build();
        var token = new RefreshToken(user, DateTime.UtcNow.AddMinutes(10), "ip");

        token.IsValid().Should().BeFalse();
    }

    [Fact]
    public void MarkReplaced_ShouldSetReplacementId()
    {
        var user = new AppUserBuilder().Build();
        var token = new RefreshToken(user, DateTime.UtcNow.AddMinutes(5), "ip");

        token.MarkReplaced("replacement-id");

        token.ReplacedBy.Should().Be("replacement-id");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkReplaced_ShouldThrow_WhenReplacementIdIsInvalid(string? invalid)
    {
        var user = new AppUserBuilder().Build();
        var token = new RefreshToken(user, DateTime.UtcNow.AddMinutes(5), "ip");

        var act = () => token.MarkReplaced(invalid!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("replacementId");
    }
}
