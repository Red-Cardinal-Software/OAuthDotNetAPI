using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs.Auth;
using Application.DTOs.Jwt;
using Application.DTOs.Mfa;
using Application.Models;
using Domain.Entities.Security;
using FluentAssertions;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests.Auth;

/// <summary>
/// Integration tests for Email MFA setup and verification endpoints.
/// </summary>
public class EmailMfaTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    #region Authorization Tests

    [Fact]
    public async Task StartEmailSetup_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/setup/email", new StartEmailMfaSetupDto
        {
            EmailAddress = "test@example.com"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VerifyEmailSetup_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/verify/email", new VerifyMfaSetupDto
        {
            Code = "12345678"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Email MFA Setup Flow Tests

    [Fact]
    public async Task StartEmailSetup_Authenticated_SendsCodeAndReturnsSetupInfo()
    {
        // Arrange
        var email = "email-mfa-setup@example.com";
        var token = await CreateUserAndGetTokenAsync(email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var mfaEmail = "mfa-verify@example.com";

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/setup/email", new StartEmailMfaSetupDto
        {
            EmailAddress = mfaEmail
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<EmailSetupDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.EmailAddress.Should().Be(mfaEmail);
        result.Data.CodeSent.Should().BeTrue();
        result.Data.MfaMethodId.Should().NotBe(Guid.Empty);
        result.Data.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        // Verify that a code was captured by the test email service
        var capturedCode = TestEmailService.GetLastCodeForEmail(mfaEmail);
        capturedCode.Should().NotBeNullOrEmpty("a verification code should have been sent");
        capturedCode.Should().HaveLength(8, "verification codes should be 8 digits");
    }

    [Fact]
    public async Task VerifyEmailSetup_WithValidCode_CompletesSetup()
    {
        // Arrange
        var email = "email-mfa-complete@example.com";
        var token = await CreateUserAndGetTokenAsync(email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var mfaEmail = "mfa-complete@example.com";

        // Start email setup
        await Client.PostAsJsonAsync("/api/v1/auth/mfa/setup/email", new StartEmailMfaSetupDto
        {
            EmailAddress = mfaEmail
        });

        // Get the verification code from test email service
        var verificationCode = TestEmailService.GetLastCodeForEmail(mfaEmail);
        verificationCode.Should().NotBeNullOrEmpty();

        // Act - Verify with the captured code
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/verify/email", new VerifyMfaSetupDto
        {
            Code = verificationCode!
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<MfaSetupCompleteDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.MfaMethodId.Should().NotBe(Guid.Empty);
        result.Data.RecoveryCodes.Should().NotBeEmpty("recovery codes should be provided");
        result.Data.VerifiedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task VerifyEmailSetup_WithInvalidCode_ReturnsError()
    {
        // Arrange
        var email = "email-mfa-invalid@example.com";
        var token = await CreateUserAndGetTokenAsync(email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var mfaEmail = "mfa-invalid@example.com";

        // Start email setup
        await Client.PostAsJsonAsync("/api/v1/auth/mfa/setup/email", new StartEmailMfaSetupDto
        {
            EmailAddress = mfaEmail
        });

        // Act - Verify with wrong code
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/verify/email", new VerifyMfaSetupDto
        {
            Code = "00000000" // Wrong code
        });

        // Assert
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<MfaSetupCompleteDto>>();
        result!.Success.Should().BeFalse("invalid verification code should fail");
        result.Message!.ToLower().Should().Contain("invalid");
    }

    [Fact]
    public async Task VerifyEmailSetup_WithoutStartingSetup_ReturnsError()
    {
        // Arrange
        var email = "email-mfa-nosetup@example.com";
        var token = await CreateUserAndGetTokenAsync(email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Try to verify without starting setup
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/verify/email", new VerifyMfaSetupDto
        {
            Code = "12345678"
        });

        // Assert
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<MfaSetupCompleteDto>>();
        result!.Success.Should().BeFalse("verification without setup should fail");
    }

    #endregion

    #region Duplicate Setup Tests

    [Fact]
    public async Task StartEmailSetup_WhenAlreadySetup_ReturnsError()
    {
        // Arrange
        var email = "email-mfa-duplicate@example.com";
        var token = await CreateUserAndGetTokenAsync(email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var mfaEmail = "mfa-dup@example.com";

        // Complete first email MFA setup
        await Client.PostAsJsonAsync("/api/v1/auth/mfa/setup/email", new StartEmailMfaSetupDto
        {
            EmailAddress = mfaEmail
        });

        var verificationCode = TestEmailService.GetLastCodeForEmail(mfaEmail);
        await Client.PostAsJsonAsync("/api/v1/auth/mfa/verify/email", new VerifyMfaSetupDto
        {
            Code = verificationCode!
        });

        // Act - Try to setup email MFA again
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/setup/email", new StartEmailMfaSetupDto
        {
            EmailAddress = "another@example.com"
        });

        // Assert
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<EmailSetupDto>>();
        result!.Success.Should().BeFalse("duplicate email MFA setup should fail");
        result.Message!.ToLower().Should().Contain("already");
    }

    [Fact]
    public async Task StartEmailSetup_ReplacesUnverifiedSetup()
    {
        // Arrange
        var email = "email-mfa-replace@example.com";
        var token = await CreateUserAndGetTokenAsync(email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var firstMfaEmail = "first-mfa@example.com";
        var secondMfaEmail = "second-mfa@example.com";

        // Start first setup but don't verify
        await Client.PostAsJsonAsync("/api/v1/auth/mfa/setup/email", new StartEmailMfaSetupDto
        {
            EmailAddress = firstMfaEmail
        });

        // Act - Start setup with different email (should replace unverified)
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/setup/email", new StartEmailMfaSetupDto
        {
            EmailAddress = secondMfaEmail
        });

        // Assert - Should succeed and use the new email
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<EmailSetupDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.EmailAddress.Should().Be(secondMfaEmail);

        // Verify the new code is for the new email
        var newCode = TestEmailService.GetLastCodeForEmail(secondMfaEmail);
        newCode.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task StartEmailSetup_WithNullEmail_ReturnsValidationError()
    {
        // Arrange
        var email = $"email-mfa-null-validation-{Guid.NewGuid():N}@example.com";
        var token = await CreateUserAndGetTokenAsync(email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Send request with null email (via empty JSON body)
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/setup/email", new { });

        // Assert - Should fail (either validation error or service error)
        if (response.StatusCode == HttpStatusCode.UnprocessableEntity ||
            response.StatusCode == HttpStatusCode.BadRequest)
        {
            // Model validation error
            response.IsSuccessStatusCode.Should().BeFalse();
        }
        else
        {
            var result = await response.Content.ReadFromJsonAsync<ServiceResponse<EmailSetupDto>>();
            result!.Success.Should().BeFalse("null email should fail");
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("abcdefgh")]
    [InlineData("123456789")] // 9 digits
    public async Task VerifyEmailSetup_WithInvalidCodeFormat_ReturnsError(string invalidCode)
    {
        // Arrange
        var email = $"email-mfa-code-validation-{Guid.NewGuid():N}@example.com";
        var token = await CreateUserAndGetTokenAsync(email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var mfaEmail = $"mfa-code-{Guid.NewGuid():N}@example.com";

        // Start setup first
        await Client.PostAsJsonAsync("/api/v1/auth/mfa/setup/email", new StartEmailMfaSetupDto
        {
            EmailAddress = mfaEmail
        });

        // Act - Verify with invalid code format
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/verify/email", new VerifyMfaSetupDto
        {
            Code = invalidCode
        });

        // Assert - Should fail (either validation error or service error)
        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            // Validation error at DTO level
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
        else
        {
            var result = await response.Content.ReadFromJsonAsync<ServiceResponse<MfaSetupCompleteDto>>();
            result!.Success.Should().BeFalse("invalid code format should fail");
        }
    }

    #endregion

    #region Email MFA in MFA Overview Tests

    [Fact]
    public async Task MfaOverview_AfterEmailSetup_ShowsEmailMethod()
    {
        // Arrange
        var email = "email-mfa-overview@example.com";
        var token = await CreateUserAndGetTokenAsync(email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var mfaEmail = "mfa-overview@example.com";

        // Complete email MFA setup
        await Client.PostAsJsonAsync("/api/v1/auth/mfa/setup/email", new StartEmailMfaSetupDto
        {
            EmailAddress = mfaEmail
        });

        var verificationCode = TestEmailService.GetLastCodeForEmail(mfaEmail);
        await Client.PostAsJsonAsync("/api/v1/auth/mfa/verify/email", new VerifyMfaSetupDto
        {
            Code = verificationCode!
        });

        // Act - Get MFA overview
        var response = await Client.GetAsync("/api/v1/auth/mfa/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<MfaOverviewDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.HasEnabledMfa.Should().BeTrue();
        result.Data.Methods.Should().Contain(m => m.Type == MfaType.Email);
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