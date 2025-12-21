using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs.Auth;
using Application.DTOs.Jwt;
using Application.Models;
using FluentAssertions;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests.Auth;

public class ProtectedEndpointTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    private const string ProtectedEndpoint = "/api/v1/auth/mfa/overview";

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync(ProtectedEndpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid-jwt-token");

        // Act
        var response = await Client.GetAsync(ProtectedEndpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ReturnsSuccess()
    {
        // Arrange - Create user and login
        var password = "TestPassword123!";
        var email = "protected-test@example.com";
        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(password)
            .WithForceResetPassword(false));

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        loginResult!.Success.Should().BeTrue("login should succeed before testing protected endpoint");

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult.Data!.Token);

        // Act
        var response = await Client.GetAsync(ProtectedEndpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_ReturnsUnauthorized()
    {
        // Arrange - Use a malformed/expired-looking JWT
        // This is a structurally valid but expired/invalid JWT
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
                          "eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IlRlc3QiLCJleHAiOjF9." +
                          "invalid-signature";

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await Client.GetAsync(ProtectedEndpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}