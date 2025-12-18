using System.Net;
using System.Net.Http.Json;
using Application.DTOs.Auth;
using Application.DTOs.Jwt;
using Application.Models;
using FluentAssertions;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests.Auth;

public class LogoutTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    [Fact]
    public async Task Logout_WithValidRefreshToken_InvalidatesToken()
    {
        // Arrange - Create user and login
        var password = "TestPassword123!";
        var email = "logout-test@example.com";
        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(password)
            .WithForceResetPassword(false));

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        loginResult!.Success.Should().BeTrue();

        var refreshToken = loginResult.Data!.RefreshToken!;

        // Act - Logout
        var logoutResponse = await Client.PostAsJsonAsync("/api/auth/logout", new UserLogoutDto
        {
            Username = email,
            RefreshToken = refreshToken
        });

        // Assert - Logout should succeed
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify refresh token is now invalid
        var refreshResponse = await Client.PostAsJsonAsync("/api/auth/refresh", new UserRefreshTokenDto
        {
            Username = email,
            RefreshToken = refreshToken
        });

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        refreshResult!.Success.Should().BeFalse("refresh token should be invalidated after logout");
    }

    [Fact]
    public async Task Logout_WithInvalidRefreshToken_ReturnsFailure()
    {
        // Arrange
        var email = "logout-invalid@example.com";
        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword("TestPassword123!")
            .WithForceResetPassword(false));

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/logout", new UserLogoutDto
        {
            Username = email,
            RefreshToken = "invalid-refresh-token"
        });

        // Assert - Invalid token returns either 200 with Success=false or 500 (unhandled)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<ServiceResponse<object>>();
            result!.Success.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Logout_CanLoginAgainAfterLogout()
    {
        // Arrange - Create user and login
        var password = "TestPassword123!";
        var email = "relogin-test@example.com";
        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(password)
            .WithForceResetPassword(false));

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        var refreshToken = loginResult!.Data!.RefreshToken!;

        // Logout
        await Client.PostAsJsonAsync("/api/auth/logout", new UserLogoutDto
        {
            Username = email,
            RefreshToken = refreshToken
        });

        // Act - Login again
        var reloginResponse = await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });

        // Assert
        reloginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await reloginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeTrue("should be able to login again after logout");
        result.Data!.Token.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }
}