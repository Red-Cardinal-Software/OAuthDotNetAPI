using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs.Auth;
using Application.DTOs.Jwt;
using Application.DTOs.Mfa;
using Application.Interfaces.Services;
using Application.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests.Auth;

public class MfaTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    #region MFA Overview Tests

    [Fact]
    public async Task GetMfaOverview_Authenticated_ReturnsOverview()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("mfa-overview@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/v1/auth/mfa/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<MfaOverviewDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.HasEnabledMfa.Should().BeFalse("new user should not have MFA enabled");
        result.Data.TotalMethods.Should().Be(0);
    }

    [Fact]
    public async Task GetMfaOverview_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/auth/mfa/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region TOTP Setup Tests

    [Fact]
    public async Task StartTotpSetup_Authenticated_ReturnsSetupData()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("totp-setup@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.PostAsync("/api/v1/auth/mfa/setup/totp", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<MfaSetupDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Secret.Should().NotBeNullOrEmpty("should return TOTP secret");
        result.Data.QrCodeUri.Should().Contain("otpauth://totp/", "should return valid OTP auth URI");
        result.Data.QrCodeImage.Should().NotBeNullOrEmpty("should return QR code image");
    }

    [Fact]
    public async Task StartTotpSetup_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsync("/api/v1/auth/mfa/setup/totp", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VerifyTotpSetup_WithValidCode_EnablesMfa()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("totp-verify@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Start TOTP setup to get the secret
        var setupResponse = await Client.PostAsync("/api/v1/auth/mfa/setup/totp", null);
        var setupResult = await setupResponse.Content.ReadFromJsonAsync<ServiceResponse<MfaSetupDto>>();
        setupResult!.Success.Should().BeTrue();

        var secret = setupResult.Data!.Secret;

        // Generate a valid TOTP code using the provider
        var totpCode = GenerateTotpCode(secret);

        var verifyRequest = new VerifyMfaSetupDto
        {
            Code = totpCode,
            Name = "My Authenticator"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/verify/totp", verifyRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<MfaSetupCompleteDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.MfaMethodId.Should().NotBeEmpty();
        result.Data.RecoveryCodes.Should().NotBeEmpty("should return recovery codes");
        result.Data.IsDefault.Should().BeTrue("first MFA method should be default");
    }

    [Fact]
    public async Task VerifyTotpSetup_WithInvalidCode_ReturnsFailure()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("totp-invalid@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Start TOTP setup
        await Client.PostAsync("/api/v1/auth/mfa/setup/totp", null);

        var verifyRequest = new VerifyMfaSetupDto
        {
            Code = "000000", // Invalid code
            Name = "My Authenticator"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/verify/totp", verifyRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<MfaSetupCompleteDto>>();
        result!.Success.Should().BeFalse("invalid code should fail verification");
    }

    #endregion

    #region MFA Method Management Tests

    [Fact]
    public async Task UpdateMfaMethod_ChangeName_Succeeds()
    {
        // Arrange
        var methodId = await SetupMfaAndGetMethodIdAsync("update-name@example.com");

        var updateRequest = new UpdateMfaMethodDto
        {
            Name = "Updated Authenticator Name"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/v1/auth/mfa/method/{methodId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<MfaMethodDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Updated Authenticator Name");
    }

    [Fact]
    public async Task DeleteMfaMethod_ExistingMethod_RemovesMethod()
    {
        // Arrange
        var methodId = await SetupMfaAndGetMethodIdAsync("delete-method@example.com");

        // Act
        var response = await Client.DeleteAsync($"/api/v1/auth/mfa/method/{methodId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify MFA is no longer enabled
        var overviewResponse = await Client.GetAsync("/api/v1/auth/mfa/overview");
        var overview = await overviewResponse.Content.ReadFromJsonAsync<ServiceResponse<MfaOverviewDto>>();
        overview!.Data!.HasEnabledMfa.Should().BeFalse("MFA should be disabled after removing only method");
    }

    [Fact]
    public async Task RegenerateRecoveryCodes_ExistingMethod_ReturnsNewCodes()
    {
        // Arrange
        var methodId = await SetupMfaAndGetMethodIdAsync("regen-codes@example.com");

        // Act
        var response = await Client.PostAsync($"/api/v1/auth/mfa/method/{methodId}/recovery-codes", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<string[]>>();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty("should return new recovery codes");
    }

    #endregion

    #region MFA Login Flow Tests

    [Fact]
    public async Task Login_WithMfaEnabled_RequiresMfaChallenge()
    {
        // Arrange - Create user with MFA enabled
        var email = "mfa-login@example.com";
        var password = "TestPassword123!";
        await SetupUserWithMfaAsync(email, password);

        // Clear auth header for login
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.RequiresMfa.Should().BeTrue("login should require MFA when enabled");
        result.Data.MfaChallenge.Should().NotBeNull();
        result.Data.MfaChallenge!.ChallengeToken.Should().NotBeNullOrEmpty();
        result.Data.Token.Should().BeNullOrEmpty("should not issue token before MFA completion");
    }

    [Fact]
    public async Task CompleteMfa_WithValidCode_ReturnsTokens()
    {
        // Arrange - Create user with MFA and get challenge
        var email = "mfa-complete@example.com";
        var password = "TestPassword123!";
        var secret = await SetupUserWithMfaAsync(email, password);

        Client.DefaultRequestHeaders.Authorization = null;

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        loginResult!.Data!.RequiresMfa.Should().BeTrue();

        var challengeToken = loginResult.Data.MfaChallenge!.ChallengeToken;
        var totpCode = GenerateTotpCode(secret);

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/complete", new CompleteMfaDto
        {
            ChallengeToken = challengeToken,
            Code = totpCode
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.Token.Should().NotBeNullOrEmpty("should issue token after MFA completion");
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CompleteMfa_WithInvalidCode_ReturnsFailure()
    {
        // Arrange
        var email = "mfa-invalid-complete@example.com";
        var password = "TestPassword123!";
        await SetupUserWithMfaAsync(email, password);

        Client.DefaultRequestHeaders.Authorization = null;

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        var challengeToken = loginResult!.Data!.MfaChallenge!.ChallengeToken;

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/complete", new CompleteMfaDto
        {
            ChallengeToken = challengeToken,
            Code = "000000" // Invalid code
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeFalse("invalid MFA code should fail");
    }

    #endregion

    #region Recovery Code Tests

    [Fact]
    public async Task CompleteMfa_WithValidRecoveryCode_ReturnsTokens()
    {
        // Arrange - Create user with MFA and get recovery codes
        var email = "recovery-login@example.com";
        var password = "TestPassword123!";
        var (_, recoveryCodes) = await SetupUserWithMfaAndGetCodesAsync(email, password);

        Client.DefaultRequestHeaders.Authorization = null;

        // Login to get MFA challenge
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        loginResult!.Data!.RequiresMfa.Should().BeTrue();

        var challengeToken = loginResult.Data.MfaChallenge!.ChallengeToken;
        var recoveryCode = recoveryCodes.First();

        // Act - Complete MFA with recovery code
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/complete", new CompleteMfaDto
        {
            ChallengeToken = challengeToken,
            Code = recoveryCode,
            IsRecoveryCode = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeTrue("valid recovery code should complete MFA");
        result.Data!.Token.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CompleteMfa_WithUsedRecoveryCode_ReturnsFailure()
    {
        // Arrange - Create user with MFA and get recovery codes
        var email = "recovery-reuse@example.com";
        var password = "TestPassword123!";
        var (_, recoveryCodes) = await SetupUserWithMfaAndGetCodesAsync(email, password);

        Client.DefaultRequestHeaders.Authorization = null;

        var recoveryCode = recoveryCodes.First();

        // First login - use the recovery code
        var firstLoginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });
        var firstLoginResult = await firstLoginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        var firstChallengeToken = firstLoginResult!.Data!.MfaChallenge!.ChallengeToken;

        var firstMfaResponse = await Client.PostAsJsonAsync("/api/v1/auth/mfa/complete", new CompleteMfaDto
        {
            ChallengeToken = firstChallengeToken,
            Code = recoveryCode,
            IsRecoveryCode = true
        });
        var firstMfaResult = await firstMfaResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        firstMfaResult!.Success.Should().BeTrue("first use of recovery code should succeed");

        // Second login - try to reuse the same recovery code
        var secondLoginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });
        var secondLoginResult = await secondLoginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        var secondChallengeToken = secondLoginResult!.Data!.MfaChallenge!.ChallengeToken;

        // Act - Try to reuse the recovery code
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/complete", new CompleteMfaDto
        {
            ChallengeToken = secondChallengeToken,
            Code = recoveryCode,
            IsRecoveryCode = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeFalse("recovery code should only be usable once");
    }

    [Fact]
    public async Task CompleteMfa_WithInvalidRecoveryCode_ReturnsFailure()
    {
        // Arrange - Create user with MFA
        var email = "recovery-invalid@example.com";
        var password = "TestPassword123!";
        await SetupUserWithMfaAsync(email, password);

        Client.DefaultRequestHeaders.Authorization = null;

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        var challengeToken = loginResult!.Data!.MfaChallenge!.ChallengeToken;

        // Act - Try with invalid recovery code
        var response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/complete", new CompleteMfaDto
        {
            ChallengeToken = challengeToken,
            Code = "INVALID-RECOVERY-CODE",
            IsRecoveryCode = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeFalse("invalid recovery code should fail");
    }

    [Fact]
    public async Task CompleteMfa_CanUseMultipleRecoveryCodes_Sequentially()
    {
        // Arrange - Create user with MFA and get recovery codes
        var email = "recovery-multiple@example.com";
        var password = "TestPassword123!";
        var (_, recoveryCodes) = await SetupUserWithMfaAndGetCodesAsync(email, password);

        Client.DefaultRequestHeaders.Authorization = null;

        // Use first recovery code
        var firstCode = recoveryCodes[0];
        var secondCode = recoveryCodes[1];

        // First login with first code
        var login1 = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });
        var login1Result = await login1.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();

        var mfa1Response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/complete", new CompleteMfaDto
        {
            ChallengeToken = login1Result!.Data!.MfaChallenge!.ChallengeToken,
            Code = firstCode,
            IsRecoveryCode = true
        });
        var mfa1Result = await mfa1Response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        mfa1Result!.Success.Should().BeTrue("first recovery code should work");

        // Second login with second code
        var login2 = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });
        var login2Result = await login2.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();

        // Act
        var mfa2Response = await Client.PostAsJsonAsync("/api/v1/auth/mfa/complete", new CompleteMfaDto
        {
            ChallengeToken = login2Result!.Data!.MfaChallenge!.ChallengeToken,
            Code = secondCode,
            IsRecoveryCode = true
        });

        // Assert
        var mfa2Result = await mfa2Response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        mfa2Result!.Success.Should().BeTrue("second recovery code should also work");
        mfa2Result.Data!.Token.Should().NotBeNullOrEmpty();
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

    private async Task<Guid> SetupMfaAndGetMethodIdAsync(string email)
    {
        var token = await CreateUserAndGetTokenAsync(email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Start TOTP setup
        var setupResponse = await Client.PostAsync("/api/v1/auth/mfa/setup/totp", null);
        var setupResult = await setupResponse.Content.ReadFromJsonAsync<ServiceResponse<MfaSetupDto>>();

        var secret = setupResult!.Data!.Secret;
        var totpCode = GenerateTotpCode(secret);

        // Verify setup
        var verifyResponse = await Client.PostAsJsonAsync("/api/v1/auth/mfa/verify/totp", new VerifyMfaSetupDto
        {
            Code = totpCode,
            Name = "Test Authenticator"
        });

        var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<ServiceResponse<MfaSetupCompleteDto>>();
        return verifyResult!.Data!.MfaMethodId;
    }

    private async Task<string> SetupUserWithMfaAsync(string email, string password)
    {
        var (secret, _) = await SetupUserWithMfaAndGetCodesAsync(email, password);
        return secret;
    }

    private async Task<(string Secret, string[] RecoveryCodes)> SetupUserWithMfaAndGetCodesAsync(string email, string password)
    {
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
        var token = loginResult!.Data!.Token!;

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Start and complete TOTP setup
        var setupResponse = await Client.PostAsync("/api/v1/auth/mfa/setup/totp", null);
        var setupResult = await setupResponse.Content.ReadFromJsonAsync<ServiceResponse<MfaSetupDto>>();
        var secret = setupResult!.Data!.Secret;

        var totpCode = GenerateTotpCode(secret);

        var verifyResponse = await Client.PostAsJsonAsync("/api/v1/auth/mfa/verify/totp", new VerifyMfaSetupDto
        {
            Code = totpCode,
            Name = "Test Authenticator"
        });

        var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<ServiceResponse<MfaSetupCompleteDto>>();
        var recoveryCodes = verifyResult!.Data!.RecoveryCodes;

        return (secret, recoveryCodes);
    }

    private string GenerateTotpCode(string secret)
    {
        // Use the same TOTP algorithm as the provider
        var secretBytes = FromBase32(secret);
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timeCounter = currentTime / 30;

        return GenerateCodeForTimeCounter(secretBytes, timeCounter, 6);
    }

    private static string GenerateCodeForTimeCounter(byte[] secret, long timeCounter, int digits)
    {
        var counterBytes = BitConverter.GetBytes(timeCounter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        using var hmac = new System.Security.Cryptography.HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[^1] & 0x0F;
        var truncatedHash = ((hash[offset] & 0x7F) << 24) |
                           ((hash[offset + 1] & 0xFF) << 16) |
                           ((hash[offset + 2] & 0xFF) << 8) |
                           (hash[offset + 3] & 0xFF);

        var code = truncatedHash % (int)Math.Pow(10, digits);
        return code.ToString().PadLeft(digits, '0');
    }

    private static byte[] FromBase32(string base32)
    {
        const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        base32 = base32.Replace(" ", "").ToUpperInvariant();

        var result = new List<byte>();
        int buffer = 0;
        int bitsLeft = 0;

        foreach (char c in base32)
        {
            int index = validChars.IndexOf(c);
            if (index < 0)
                throw new ArgumentException($"Invalid Base32 character: {c}");

            buffer = (buffer << 5) | index;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                result.Add((byte)((buffer >> (bitsLeft - 8)) & 0xFF));
                bitsLeft -= 8;
            }
        }

        return result.ToArray();
    }

    #endregion
}