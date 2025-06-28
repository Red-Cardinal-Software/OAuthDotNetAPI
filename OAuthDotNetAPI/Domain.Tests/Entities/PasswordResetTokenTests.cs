using Domain.Entities.Identity;
using FluentAssertions;
using Infrastructure.Security;
using TestUtils.EntityBuilders;
using Xunit;

namespace Domain.Tests.Entities;

public class PasswordResetTokenTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithValidData()
    {
        var user = new AppUserBuilder().Build();
        var expiration = DateTime.UtcNow.AddHours(1);
        var ip = "127.0.0.1";

        var token = new PasswordResetToken(user, expiration, ip);

        token.Id.Should().NotBe(Guid.Empty);
        token.AppUser.Should().Be(user);
        token.AppUserId.Should().Be(user.Id);
        token.Expiration.Should().Be(expiration);
        token.CreatedByIp.Should().Be(ip);
        token.ClaimedDate.Should().BeNull();
        token.ClaimedByIp.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenUserIsNull()
    {
        var expiration = DateTime.UtcNow.AddHours(1);
        var ip = "127.0.0.1";

        var act = () => new PasswordResetToken(null!, expiration, ip);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenIpIsNull()
    {
        var user = new AppUserBuilder().Build();
        var expiration = DateTime.UtcNow.AddHours(1);

        var act = () => new PasswordResetToken(user, expiration, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Claim_ShouldMarkTokenAsClaimed_AndUpdatePassword()
    {
        var user = new AppUserBuilder().WithPasswordHash("old").Build();
        var token = new PasswordResetToken(user, DateTime.UtcNow.AddHours(1), "127.0.0.1");

        token.Claim("newHashedPassword1234567890", "8.8.8.8");

        token.IsClaimed().Should().BeTrue();
        token.ClaimedByIp.Should().Be("8.8.8.8");
        token.ClaimedDate.Should().NotBeNull();
        user.Password.Value.Should().Be("newHashedPassword1234567890");
    }

    [Fact]
    public void Claim_ShouldThrow_WhenAlreadyClaimed()
    {
        var user = new AppUserBuilder().Build();
        var token = new PasswordResetToken(user, DateTime.UtcNow.AddHours(1), "127.0.0.1");

        token.Claim("hashed1234567890abcdefg", "1.1.1.1");

        var act = () => token.Claim("another", "2.2.2.2");

        act.Should().Throw<InvalidOperationException>().WithMessage("Token already claimed.");
    }

    [Fact]
    public void Claim_ShouldThrow_WhenIpIsNull()
    {
        var user = new AppUserBuilder().Build();
        var token = new PasswordResetToken(user, DateTime.UtcNow.AddHours(1), "127.0.0.1");

        var act = () => token.Claim("password", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ClaimRedundantToken_ShouldThrow_WhenAlreadyClaimed()
    {
        var user = new AppUserBuilder().Build();
        var token = new PasswordResetToken(user, DateTime.UtcNow.AddHours(1), "init");

        token.ClaimRedundantToken("ip1");

        var act = () => token.ClaimRedundantToken("ip2");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_BeforeExpiration()
    {
        var token = new PasswordResetToken(new AppUserBuilder().Build(), DateTime.UtcNow.AddMinutes(10), "ip");
        token.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_AfterExpiration()
    {
        var token = new PasswordResetToken(new AppUserBuilder().Build(), DateTime.UtcNow.AddSeconds(-5), "ip");
        token.IsExpired().Should().BeTrue();
    }
}