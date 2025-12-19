using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Application.DTOs.Auth;
using Application.DTOs.Jwt;
using Application.Models;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests.Auth;

/// <summary>
/// Integration tests for JWT token validation, refresh, and revocation.
/// </summary>
public class JwtTokenValidationTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    #region Missing/Invalid Token Tests

    [Fact]
    public async Task ProtectedEndpoint_WithNoToken_ReturnsUnauthorized()
    {
        // Arrange - No authorization header set

        // Act
        var response = await Client.GetAsync("/api/v1/auth/mfa/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithEmptyBearerToken_ReturnsUnauthorized()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "");

        // Act
        var response = await Client.GetAsync("/api/v1/auth/mfa/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithMalformedToken_ReturnsUnauthorized()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.valid.jwt.token");

        // Act
        var response = await Client.GetAsync("/api/v1/auth/mfa/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithRandomBase64Token_ReturnsUnauthorized()
    {
        // Arrange - Create a random base64 string that looks like a JWT structure
        var fakeToken = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\"}")) + "." +
                        Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"sub\":\"fake\"}")) + "." +
                        Convert.ToBase64String(Encoding.UTF8.GetBytes("fakesignature"));

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fakeToken);

        // Act
        var response = await Client.GetAsync("/api/v1/auth/mfa/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Tampered Token Tests

    [Fact]
    public async Task ProtectedEndpoint_WithTamperedPayload_ReturnsUnauthorized()
    {
        // Arrange - Get a valid token first
        var (token, _) = await CreateUserAndGetTokensAsync("jwt-tampered@example.com");

        // Tamper with the payload by modifying the middle section
        var parts = token.Split('.');
        parts.Should().HaveCount(3, "JWT should have 3 parts");

        // Decode payload, modify, re-encode
        var payload = Base64UrlDecode(parts[1]);
        var tamperedPayload = payload.Replace("jwt-tampered@example.com", "admin@example.com");
        parts[1] = Base64UrlEncode(tamperedPayload);

        var tamperedToken = string.Join(".", parts);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        // Act
        var response = await Client.GetAsync("/api/v1/auth/mfa/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithWrongSigningKey_ReturnsUnauthorized()
    {
        // Arrange - Create a token signed with a different key
        var tokenHandler = new JwtSecurityTokenHandler();
        var wrongKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsACompletelyDifferentSecretKeyThatIsLongEnough!"));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, "hacker@example.com")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var wronglySignedToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wronglySignedToken);

        // Act
        var response = await Client.GetAsync("/api/v1/auth/mfa/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Token Refresh Tests

    [Fact]
    public async Task Refresh_WithValidRefreshToken_ReturnsNewTokens()
    {
        // Arrange
        var email = "jwt-refresh-valid@example.com";
        var (_, refreshToken) = await CreateUserAndGetTokensAsync(email);

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new UserRefreshTokenDto
        {
            Username = email,
            RefreshToken = refreshToken
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.Token.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Refresh_WithInvalidRefreshToken_ReturnsError()
    {
        // Arrange
        var email = "jwt-refresh-invalid@example.com";
        await CreateUserAndGetTokensAsync(email);

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new UserRefreshTokenDto
        {
            Username = email,
            RefreshToken = "invalid-refresh-token"
        });

        // Assert - Should fail (either non-success status code or ServiceResponse with Success=false)
        if (!response.IsSuccessStatusCode)
        {
            // Server returned error status - that's a failure, which is what we want
            response.IsSuccessStatusCode.Should().BeFalse("invalid refresh token should fail");
            return;
        }

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeFalse("invalid refresh token should fail");
    }

    [Fact]
    public async Task Refresh_WithWrongUsername_ReturnsError()
    {
        // Arrange
        var email = "jwt-refresh-wronguser@example.com";
        var (_, refreshToken) = await CreateUserAndGetTokensAsync(email);

        // Act - Try to use refresh token with different username
        var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new UserRefreshTokenDto
        {
            Username = "different@example.com",
            RefreshToken = refreshToken
        });

        // Assert
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeFalse("refresh token should be bound to specific user");
    }

    [Fact]
    public async Task Refresh_WithAccessTokenAsRefreshToken_ReturnsError()
    {
        // Arrange
        var email = "jwt-access-as-refresh@example.com";
        var (accessToken, _) = await CreateUserAndGetTokensAsync(email);

        // Act - Try to use access token as refresh token
        var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new UserRefreshTokenDto
        {
            Username = email,
            RefreshToken = accessToken
        });

        // Assert - Should fail (either non-success status code or ServiceResponse with Success=false)
        if (!response.IsSuccessStatusCode)
        {
            // Server returned error status - that's a failure, which is what we want
            response.IsSuccessStatusCode.Should().BeFalse("access token should not work as refresh token");
            return;
        }

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeFalse("access token should not work as refresh token");
    }

    [Fact]
    public async Task NewAccessToken_FromRefresh_WorksOnProtectedEndpoint()
    {
        // Arrange
        var email = "jwt-new-token-works@example.com";
        var (_, refreshToken) = await CreateUserAndGetTokensAsync(email);

        // Refresh to get new token
        var refreshResponse = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new UserRefreshTokenDto
        {
            Username = email,
            RefreshToken = refreshToken
        });

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        var newToken = refreshResult!.Data!.Token;

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

        // Act - Use new token on protected endpoint
        var response = await Client.GetAsync("/api/v1/auth/mfa/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Token Revocation Tests

    [Fact]
    public async Task Refresh_AfterLogout_ReturnsError()
    {
        // Arrange
        var email = "jwt-logout@example.com";
        var (_, refreshToken) = await CreateUserAndGetTokensAsync(email);

        // Logout first
        await Client.PostAsJsonAsync("/api/v1/auth/logout", new UserLogoutDto
        {
            Username = email,
            RefreshToken = refreshToken
        });

        // Act - Try to refresh after logout
        var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new UserRefreshTokenDto
        {
            Username = email,
            RefreshToken = refreshToken
        });

        // Assert
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeFalse("refresh token should be invalidated after logout");
    }

    #endregion

    #region Valid Token Tests

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        var email = "jwt-valid@example.com";
        var (token, _) = await CreateUserAndGetTokensAsync(email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/v1/auth/mfa/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MultipleProtectedRequests_WithSameToken_AllSucceed()
    {
        // Arrange
        var email = "jwt-multi-request@example.com";
        var (token, _) = await CreateUserAndGetTokensAsync(email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Make multiple requests
        var response1 = await Client.GetAsync("/api/v1/auth/mfa/overview");
        var response2 = await Client.GetAsync("/api/v1/auth/mfa/overview");
        var response3 = await Client.GetAsync("/api/v1/auth/mfa/overview");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        response3.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Authorization Scheme Tests

    [Fact]
    public async Task ProtectedEndpoint_WithBasicAuth_ReturnsUnauthorized()
    {
        // Arrange - Use Basic auth instead of Bearer
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:password"));
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        // Act
        var response = await Client.GetAsync("/api/v1/auth/mfa/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTokenInQueryString_ReturnsUnauthorized()
    {
        // Arrange
        var (token, _) = await CreateUserAndGetTokensAsync("jwt-querystring@example.com");

        // Act - Token in query string should not work (tokens should be in Authorization header)
        var response = await Client.GetAsync($"/api/v1/auth/mfa/overview?access_token={token}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helper Methods

    private async Task<(string accessToken, string refreshToken)> CreateUserAndGetTokensAsync(string email)
    {
        var password = "TestPassword123!";
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
        return (loginResult!.Data!.Token!, loginResult.Data.RefreshToken!);
    }

    private static string Base64UrlDecode(string input)
    {
        var padded = input.Length % 4 == 0 ? input : input + new string('=', 4 - input.Length % 4);
        var base64 = padded.Replace('-', '+').Replace('_', '/');
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    private static string Base64UrlEncode(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    #endregion
}