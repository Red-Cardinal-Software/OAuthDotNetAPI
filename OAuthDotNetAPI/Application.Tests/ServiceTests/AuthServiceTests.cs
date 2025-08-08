using Application.Common.Constants;
using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Application.Interfaces.Security;
using Application.Interfaces.Services;
using Application.Logging;
using Application.Services.Auth;
using Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TestUtils.EntityBuilders;
using TestUtils.Utilities;
using Xunit;

namespace Application.Tests.ServiceTests;

public class AuthServiceTests
{
    private readonly Mock<IAppUserRepository> _userRepo = new();
    private readonly Mock<IPasswordResetEmailService> _emailService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IPasswordResetService> _passwordResetService = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IRoleRepository> _roleRepository = new();
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepository = new();
    private readonly Mock<ILogger<AuthService>> _mockLogger = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var logger = new LogContextHelper<AuthService>(_mockLogger.Object);
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"AppSettings-Token", "k<tS6l6;<{{P#'iI5vW8KZon7o7*_>j&V)b9<:&jB[_#wb[#GSm/$t<<u<=!#&|5@0M()Y"}
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _authService = new AuthService(
            _userRepo.Object,
            _emailService.Object,
            _passwordHasher.Object,
            _unitOfWork.Object,
            _passwordResetService.Object,
            _refreshTokenRepository.Object,
            _roleRepository.Object,
            _passwordResetTokenRepository.Object,
            logger,
            config
        );
    }

    [Fact]
    public async Task Login_WithInvalidUser_ReturnsError()
    {
        // Arrange
        _userRepo.Setup(x => x.UserExistsAsync("fakeuser")).ReturnsAsync(false);

        // Act
        var result = await _authService.Login("fakeuser", "wrongpassword", "127.0.0.1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ServiceResponseConstants.UsernameOrPasswordIncorrect);
    }

    [Fact]
    public async Task Login_WithValidUser_ReturnsSuccess()
    {
        // Arrange
        var user = new AppUserBuilder().Build();
        var refreshToken = new RefreshToken(user, DateTime.UtcNow.AddMinutes(5), "10.0.0.1");

        _userRepo.Setup(x => x.UserExistsAsync("testuser")).ReturnsAsync(true);
        _userRepo.Setup(x => x.GetUserByUsernameAsync("testuser")).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _refreshTokenRepository.Setup(x =>
            x.SaveRefreshTokenAsync(refreshToken)
        ).ReturnsAsync(refreshToken);

        // Act
        var result = await _authService.Login("testuser", TestConstants.Passwords.Default, "127.0.0.1");

        // Assert
        result.Success.Should().BeTrue();
        result.Data?.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsError()
    {
        var user = new AppUserBuilder().Build();

        _userRepo.Setup(x => x.UserExistsAsync("testuser")).ReturnsAsync(true);
        _userRepo.Setup(x => x.GetUserByUsernameAsync("testuser")).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify("wrongpass", "hashedpass")).Returns(false);

        var result = await _authService.Login("testuser", "wrongpass", "127.0.0.1");

        result.Success.Should().BeFalse();
        result.Message.Should().Be(ServiceResponseConstants.UsernameOrPasswordIncorrect);
    }

    [Fact]
    public async Task RequestPasswordReset_WithUnknownEmail_ReturnsSuccessWithNoLeak()
    {
        // Arrange
        _userRepo.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((AppUser?)null);

        // Act
        var result = await _authService.RequestPasswordReset("unknown@example.com", "127.0.0.1");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
        result.Message.Should().Be(ServiceResponseConstants.EmailPasswordResetSent);
    }

    [Fact]
    public async Task Logout_WithValidToken_RevokesFamily()
    {
        // Arrange
        var refreshTokenId = Guid.NewGuid();
        var tokenFamilyId = Guid.NewGuid();

        var user = new AppUserBuilder().Build();
        var refreshToken = new RefreshToken(user, DateTime.UtcNow.AddMinutes(1), "10.0.0.1", tokenFamilyId);

        _userRepo.Setup(x => x.GetUserByUsernameAsync(user.Username)).ReturnsAsync(user);
        _refreshTokenRepository.Setup(x => x.GetRefreshTokenAsync(refreshTokenId, user.Id)).ReturnsAsync(refreshToken);
        _refreshTokenRepository.Setup(x => x.RevokeRefreshTokenFamilyAsync(tokenFamilyId)).ReturnsAsync(true);

        // Act
        var result = await _authService.Logout(user.Username, refreshTokenId.ToString());

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task Logout_WithInvalidUser_ReturnsUnauthorized()
    {
        // Arrange
        _userRepo.Setup(x => x.GetUserByUsernameAsync("unknown")).ReturnsAsync((AppUser?)null);

        // Act
        var result = await _authService.Logout("unknown", Guid.NewGuid().ToString());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ServiceResponseConstants.UserUnauthorized);
    }

    [Fact]
    public async Task Logout_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var user = new AppUserBuilder().Build();

        _userRepo.Setup(x => x.GetUserByUsernameAsync(user.Username)).ReturnsAsync(user);
        _refreshTokenRepository.Setup(x => x.GetRefreshTokenAsync(It.IsAny<Guid>(), user.Id)).ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _authService.Logout(user.Username, Guid.NewGuid().ToString());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ServiceResponseConstants.UserUnauthorized);
    }

    [Fact]
    public async Task Refresh_WithReusedToken_RevokesFamilyAndFails()
    {
        // Arrange
        var appUser = new AppUserBuilder().Build();
        var tokenFamilyId = Guid.NewGuid();

        var usedToken = new RefreshToken(appUser, DateTime.UtcNow.AddMinutes(10), "127.0.0.1", tokenFamilyId, "already-used");

        _userRepo.Setup(x => x.UserExistsAsync(appUser.Username)).ReturnsAsync(true);
        _userRepo.Setup(x => x.GetUserByUsernameAsync(appUser.Username)).ReturnsAsync(appUser);
        _refreshTokenRepository.Setup(x => x.GetRefreshTokenAsync(It.IsAny<Guid>(), appUser.Id)).ReturnsAsync(usedToken);
        _refreshTokenRepository.Setup(x => x.RevokeRefreshTokenFamilyAsync(tokenFamilyId)).ReturnsAsync(true);

        // Act
        var result = await _authService.Refresh(appUser.Username, usedToken.Id.ToString(), "127.0.0.1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(ServiceResponseConstants.UserUnauthorized);
    }

}
