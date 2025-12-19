using System.Net;
using System.Net.Http.Json;
using Application.DTOs.Auth;
using Application.DTOs.Jwt;
using Application.Models;
using FluentAssertions;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests.Auth;

public class LoginTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        // Arrange
        var password = "TestPassword123!";
        await CreateTestUserAsync(u => u
            .WithEmail("login-test@example.com")
            .WithPassword(password)
            .WithForceResetPassword(false));

        var loginRequest = new UserLoginDto
        {
            Username = "login-test@example.com",
            Password = password
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Token.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsFailure()
    {
        // Arrange
        await CreateTestUserAsync(u => u
            .WithEmail("bad-password@example.com")
            .WithPassword("CorrectPassword123!"));

        var loginRequest = new UserLoginDto
        {
            Username = "bad-password@example.com",
            Password = "WrongPassword!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var loginRequest = new UserLoginDto
        {
            Username = "nobody@example.com",
            Password = "SomePassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithForceResetPassword_ReturnsForceResetFlag()
    {
        // Arrange
        var password = "TempPassword123!";
        await CreateTestUserAsync(u => u
            .WithEmail("login-force-reset@example.com")
            .WithPassword(password)
            .WithForceResetPassword(true));

        var loginRequest = new UserLoginDto
        {
            Username = "login-force-reset@example.com",
            Password = password
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.ForceReset.Should().BeTrue();
    }
}