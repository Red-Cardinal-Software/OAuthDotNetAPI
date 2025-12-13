using Application.Common.Configuration;
using Application.Common.Constants;
using Application.Interfaces.Security;
using Application.Validators;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Application.Tests.ValidatorTests;

public class PasswordValidatorTests
{
    private readonly Mock<IBlacklistedPasswordRepository> _blacklistRepo = new();
    private readonly PasswordValidator _validator;

    public PasswordValidatorTests()
    {
        // Create AppOptions for the test
        var appOptions = new AppOptions
        {
            AppName = "Starbase Template API (Test)",
            JwtSigningKey = "test-key-that-is-at-least-32-characters-long",
            JwtIssuer = "https://localhost:5001",
            JwtAudience = "starbase-api-users-test",
            JwtExpirationTimeMinutes = 15,
            RefreshTokenExpirationTimeHours = 24,
            PasswordResetExpirationTimeHours = 1,
            PasswordMinimumLength = 8,
            PasswordMaximumLength = 32
        };

        var mockAppOptions = new Mock<IOptions<AppOptions>>();
        mockAppOptions.Setup(x => x.Value).Returns(appOptions);

        _blacklistRepo.Setup(r => r.IsPasswordBlacklistedAsync(It.IsAny<string>())).ReturnsAsync(false);

        _validator = new PasswordValidator(mockAppOptions.Object, _blacklistRepo.Object);
    }

    [Fact]
    public async Task ValidPassword_ShouldPass()
    {
        var result = await _validator.ValidateAsync("ValidPassword123!");
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyPassword_ShouldFail()
    {
        var result = await _validator.ValidateAsync("");
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == ServiceResponseConstants.PasswordMustNotBeEmpty);
    }

    [Fact]
    public async Task TooShortPassword_ShouldFail()
    {
        var result = await _validator.ValidateAsync("123");
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == ServiceResponseConstants.PasswordDoesNotMeetMinimumLengthRequirements);
    }

    [Fact]
    public async Task TooLongPassword_ShouldFail()
    {
        var longPassword = new string('x', 100); // exceeds a max of 32
        var result = await _validator.ValidateAsync(longPassword);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == ServiceResponseConstants.PasswordExceedsMaximumLengthRequirements);
    }

    [Fact]
    public async Task BlacklistedPassword_ShouldFail()
    {
        const string blacklisted = "Password123!";
        _blacklistRepo.Setup(r => r.IsPasswordBlacklistedAsync(blacklisted)).ReturnsAsync(true);

        var result = await _validator.ValidateAsync(blacklisted);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == ServiceResponseConstants.PasswordIsBlacklisted);
    }
}
