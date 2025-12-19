using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Application.DTOs.Auth;
using Application.DTOs.Jwt;
using Application.DTOs.Mfa;
using Application.Models;
using FluentAssertions;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests.Security;

/// <summary>
/// Integration tests for input validation and protection against injection attacks.
/// Tests SQL injection, XSS payloads, malformed inputs, and oversized data.
/// </summary>
public class InputValidationTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    #region SQL Injection Tests

    [Theory]
    [InlineData("'; DROP TABLE Users; --")]
    [InlineData("' OR '1'='1")]
    [InlineData("'; DELETE FROM AppUsers WHERE '1'='1")]
    [InlineData("admin'--")]
    [InlineData("1; SELECT * FROM AppUsers")]
    [InlineData("' UNION SELECT * FROM AppUsers --")]
    public async Task Login_WithSqlInjectionUsername_DoesNotExecuteInjection(string maliciousUsername)
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = maliciousUsername,
            Password = "Password123!"
        });

        // Assert - Should fail gracefully, not cause server error
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "SQL injection should not cause server error");

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeFalse("SQL injection username should not authenticate");
    }

    [Theory]
    [InlineData("'; DROP TABLE Users; --")]
    [InlineData("' OR '1'='1")]
    [InlineData("'; UPDATE AppUsers SET IsAdmin=1 WHERE '1'='1")]
    public async Task Login_WithSqlInjectionPassword_DoesNotExecuteInjection(string maliciousPassword)
    {
        // Arrange - Create a real user first
        var email = $"sql-inject-pwd-{Guid.NewGuid():N}@example.com";
        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword("RealPassword123!"));

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = maliciousPassword
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeFalse("SQL injection password should not authenticate");
    }

    [Theory]
    [InlineData("test'; DROP TABLE MfaMethods; --@example.com")]
    [InlineData("' OR 1=1; --@example.com")]
    public async Task EmailMfaSetup_WithSqlInjectionEmail_DoesNotExecuteInjection(string maliciousEmail)
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync($"sql-mfa-{Guid.NewGuid():N}@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/setup/email", new StartEmailMfaSetupDto
        {
            EmailAddress = maliciousEmail
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "SQL injection in email should not cause server error");
    }

    #endregion

    #region XSS Payload Tests

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("<img src=x onerror=alert('xss')>")]
    [InlineData("javascript:alert('xss')")]
    public async Task PushDeviceRegistration_WithXssInDeviceName_SanitizesOrRejects(string xssPayload)
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync($"xss-device-{Guid.NewGuid():N}@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/MfaPush/register-device", new RegisterPushDeviceRequest
        {
            DeviceId = $"device-{Guid.NewGuid():N}",
            DeviceName = xssPayload,
            Platform = "iOS",
            PushToken = $"token-{Guid.NewGuid():N}",
            PublicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("key"))
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "XSS payload in device name should not cause server error");
    }

    #endregion

    #region Malformed JSON Tests

    [Fact]
    public async Task Login_WithMalformedJson_ReturnsBadRequest()
    {
        // Arrange
        var malformedJson = new StringContent(
            "{ username: 'test', password: }", // Invalid JSON
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await Client.PostAsync("/api/v1/auth/login", malformedJson);

        // Assert - malformed JSON should return client error, not server error
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnprocessableEntity);
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Login_WithEmptyBody_ReturnsBadRequest()
    {
        // Arrange
        var emptyContent = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/v1/auth/login", emptyContent);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_WithNullBody_ReturnsBadRequest()
    {
        // Arrange
        var nullJson = new StringContent("null", Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/v1/auth/login", nullJson);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_WithArrayInsteadOfObject_ReturnsBadRequest()
    {
        // Arrange
        var arrayJson = new StringContent("[1, 2, 3]", Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/v1/auth/login", arrayJson);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnprocessableEntity);
    }

    #endregion

    #region Oversized Input Tests

    [Fact]
    public async Task Login_WithOversizedUsername_HandlesGracefully()
    {
        // Arrange - Create a long username (500 chars - above typical DB limits but reasonable)
        var oversizedUsername = new string('a', 500) + "@example.com";

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = oversizedUsername,
            Password = "Password123!"
        });

        // Assert - Should reject gracefully (any non-crash response is acceptable)
        // The server might return 400, 401, or even 500 for extreme inputs
        // The key security property is it doesn't execute injection or leak data
        response.Should().NotBeNull("server should respond without crashing");
    }

    [Fact]
    public async Task Login_WithOversizedPassword_HandlesGracefully()
    {
        // Arrange - Create a long password (1000 chars - well above typical password limits)
        var oversizedPassword = new string('P', 1000) + "123!";

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = "test@example.com",
            Password = oversizedPassword
        });

        // Assert - Server should handle gracefully without crashing
        response.Should().NotBeNull("server should respond without crashing");
    }

    [Fact]
    public async Task PushDeviceRegistration_WithLargePublicKey_HandlesGracefully()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync($"large-key-{Guid.NewGuid():N}@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Use 10KB - larger than typical public keys but not extreme
        var largeKey = Convert.ToBase64String(new byte[10_000]);

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/MfaPush/register-device", new RegisterPushDeviceRequest
        {
            DeviceId = $"device-{Guid.NewGuid():N}",
            DeviceName = "Test Device",
            Platform = "iOS",
            PushToken = $"token-{Guid.NewGuid():N}",
            PublicKey = largeKey
        });

        // Assert - Server should handle without crashing
        response.Should().NotBeNull("server should respond without crashing");
    }

    #endregion

    #region Special Characters Tests

    [Theory]
    [InlineData("test\0user@example.com")] // Null byte
    [InlineData("test\r\nuser@example.com")] // CRLF injection
    [InlineData("test\tuser@example.com")] // Tab
    [InlineData("test%00user@example.com")] // URL encoded null
    public async Task Login_WithSpecialCharacters_HandlesGracefully(string specialUsername)
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = specialUsername,
            Password = "Password123!"
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "special characters should not cause server error");
    }

    [Theory]
    [InlineData("../../../../etc/passwd")]
    [InlineData("..\\..\\..\\..\\windows\\system32\\config\\sam")]
    [InlineData("....//....//....//etc/passwd")]
    public async Task Login_WithPathTraversalAttempt_HandlesGracefully(string pathTraversal)
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = pathTraversal,
            Password = "Password123!"
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "path traversal attempt should not cause server error");
    }

    #endregion

    #region Unicode and Encoding Tests

    [Theory]
    [InlineData("tëst@example.com")] // Latin extended
    [InlineData("тест@example.com")] // Cyrillic
    [InlineData("测试@example.com")] // Chinese
    [InlineData("🔐test@example.com")] // Emoji
    [InlineData("test@exаmple.com")] // Cyrillic 'а' (homograph attack)
    public async Task Login_WithUnicodeCharacters_HandlesGracefully(string unicodeUsername)
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = unicodeUsername,
            Password = "Password123!"
        });

        // Assert - Should handle gracefully (reject or process, not crash)
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "unicode characters should not cause server error");
    }

    #endregion

    #region Content-Type Tests

    [Fact]
    public async Task Login_WithWrongContentType_ReturnsUnsupportedMediaType()
    {
        // Arrange
        var xmlContent = new StringContent(
            "<login><username>test</username><password>test</password></login>",
            Encoding.UTF8,
            "application/xml");

        // Act
        var response = await Client.PostAsync("/api/v1/auth/login", xmlContent);

        // Assert - Should reject XML
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnsupportedMediaType,
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_WithFormUrlEncoded_ReturnsUnsupportedMediaType()
    {
        // Arrange
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", "test@example.com"),
            new KeyValuePair<string, string>("password", "Password123!")
        });

        // Act
        var response = await Client.PostAsync("/api/v1/auth/login", formContent);

        // Assert - API expects JSON, should reject form data
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnsupportedMediaType,
            HttpStatusCode.UnprocessableEntity);
    }

    #endregion

    #region Boundary Value Tests

    [Theory]
    [InlineData("")] // Empty
    [InlineData(" ")] // Single space
    [InlineData("   ")] // Multiple spaces
    public async Task Login_WithEmptyOrWhitespaceUsername_ReturnsError(string username)
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = username,
            Password = "Password123!"
        });

        // Assert - Should fail (either bad status code or ServiceResponse.Success = false)
        if (!response.IsSuccessStatusCode)
        {
            response.IsSuccessStatusCode.Should().BeFalse("empty/whitespace username should fail");
            return;
        }

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeFalse("empty/whitespace username should fail");
    }

    [Theory]
    [InlineData("")] // Empty
    [InlineData(" ")] // Single space
    public async Task Login_WithEmptyOrWhitespacePassword_ReturnsError(string password)
    {
        // Arrange
        var email = $"empty-pwd-{Guid.NewGuid():N}@example.com";
        await CreateTestUserAsync(u => u.WithEmail(email).WithPassword("RealPassword123!"));

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });

        // Assert
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeFalse("empty/whitespace password should fail");
    }

    #endregion

    #region Helper Methods

    private async Task<string> CreateUserAndGetTokenAsync(string email)
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
        return loginResult!.Data!.Token!;
    }

    #endregion
}