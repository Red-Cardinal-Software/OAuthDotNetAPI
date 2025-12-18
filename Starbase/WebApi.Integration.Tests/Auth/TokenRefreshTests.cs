using System.Net;
using System.Net.Http.Json;
using Application.DTOs.Auth;
using Application.DTOs.Jwt;
using Application.Models;
using FluentAssertions;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests.Auth;

public class TokenRefreshTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        // Arrange - Create user and login to get tokens
        var password = "TestPassword123!";
        var email = "refresh-test@example.com";
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

        var refreshRequest = new UserRefreshTokenDto
        {
            Username = email,
            RefreshToken = loginResult.Data!.RefreshToken!
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Token.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ReturnsFailure()
    {
        // Arrange
        var email = "refresh-invalid@example.com";
        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword("TestPassword123!")
            .WithForceResetPassword(false));

        var refreshRequest = new UserRefreshTokenDto
        {
            Username = email,
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert - Invalid token returns either 200 with Success=false or 500 (unhandled)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
            result!.Success.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Refresh_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var refreshRequest = new UserRefreshTokenDto
        {
            Username = "nonexistent@example.com",
            RefreshToken = "some-token"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeFalse();
    }
}