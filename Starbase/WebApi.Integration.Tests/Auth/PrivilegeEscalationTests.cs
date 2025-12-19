using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs.Auth;
using Application.DTOs.Jwt;
using Application.DTOs.Mfa;
using Application.Models;
using FluentAssertions;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests.Auth;

/// <summary>
/// Integration tests for privilege escalation and IDOR (Insecure Direct Object Reference) vulnerabilities.
/// Ensures users cannot access or modify other users' resources.
/// </summary>
public class PrivilegeEscalationTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    #region MFA Method IDOR Tests

    [Fact]
    public async Task RemoveMfaMethod_OtherUsersMfaMethod_ReturnsError()
    {
        // Arrange - User 1 sets up MFA
        var user1Email = "idor-mfa-user1@example.com";
        var user1Token = await CreateUserAndGetTokenAsync(user1Email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);

        // User 1 starts email MFA setup
        var mfaEmail = "user1-mfa@example.com";
        var setupResponse = await Client.PostAsJsonAsync("/api/v1/auth/mfa/setup/email", new StartEmailMfaSetupDto
        {
            EmailAddress = mfaEmail
        });

        var setupResult = await setupResponse.Content.ReadFromJsonAsync<ServiceResponse<EmailSetupDto>>();
        var methodId = setupResult!.Data!.MfaMethodId;

        // Verify the MFA setup
        var verificationCode = TestEmailService.GetLastCodeForEmail(mfaEmail);
        await Client.PostAsJsonAsync("/api/v1/auth/mfa/verify/email", new VerifyMfaSetupDto
        {
            Code = verificationCode!
        });

        // Arrange - User 2 logs in
        var user2Email = "idor-mfa-user2@example.com";
        var user2Token = await CreateUserAndGetTokenAsync(user2Email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2Token);

        // Act - User 2 tries to delete User 1's MFA method
        var response = await Client.DeleteAsync($"/api/v1/auth/mfa/method/{methodId}");

        // Assert - Should fail (user cannot delete another user's MFA method)
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Data.Should().BeFalse("user should not be able to delete another user's MFA method");
    }

    [Fact]
    public async Task UpdateMfaMethod_OtherUsersMfaMethod_ReturnsError()
    {
        // Arrange - User 1 sets up MFA
        var user1Email = "idor-update-mfa-user1@example.com";
        var user1Token = await CreateUserAndGetTokenAsync(user1Email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);

        // User 1 starts email MFA setup
        var mfaEmail = "update-mfa-user1@example.com";
        var setupResponse = await Client.PostAsJsonAsync("/api/v1/auth/mfa/setup/email", new StartEmailMfaSetupDto
        {
            EmailAddress = mfaEmail
        });

        var setupResult = await setupResponse.Content.ReadFromJsonAsync<ServiceResponse<EmailSetupDto>>();
        var methodId = setupResult!.Data!.MfaMethodId;

        // Verify the MFA setup
        var verificationCode = TestEmailService.GetLastCodeForEmail(mfaEmail);
        await Client.PostAsJsonAsync("/api/v1/auth/mfa/verify/email", new VerifyMfaSetupDto
        {
            Code = verificationCode!
        });

        // Arrange - User 2 logs in
        var user2Email = "idor-update-mfa-user2@example.com";
        var user2Token = await CreateUserAndGetTokenAsync(user2Email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2Token);

        // Act - User 2 tries to update User 1's MFA method
        var response = await Client.PutAsJsonAsync($"/api/v1/auth/mfa/method/{methodId}", new UpdateMfaMethodDto
        {
            Name = "Hacked by User 2"
        });

        // Assert - Should fail
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<MfaMethodDto>>();
        result!.Success.Should().BeFalse("user should not be able to update another user's MFA method");
    }

    [Fact]
    public async Task RegenerateRecoveryCodes_OtherUsersMfaMethod_ReturnsError()
    {
        // Arrange - User 1 sets up MFA
        var user1Email = "idor-recovery-user1@example.com";
        var user1Token = await CreateUserAndGetTokenAsync(user1Email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);

        // User 1 starts email MFA setup
        var mfaEmail = "recovery-user1@example.com";
        var setupResponse = await Client.PostAsJsonAsync("/api/v1/auth/mfa/setup/email", new StartEmailMfaSetupDto
        {
            EmailAddress = mfaEmail
        });

        var setupResult = await setupResponse.Content.ReadFromJsonAsync<ServiceResponse<EmailSetupDto>>();
        var methodId = setupResult!.Data!.MfaMethodId;

        // Verify the MFA setup
        var verificationCode = TestEmailService.GetLastCodeForEmail(mfaEmail);
        await Client.PostAsJsonAsync("/api/v1/auth/mfa/verify/email", new VerifyMfaSetupDto
        {
            Code = verificationCode!
        });

        // Arrange - User 2 logs in
        var user2Email = "idor-recovery-user2@example.com";
        var user2Token = await CreateUserAndGetTokenAsync(user2Email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2Token);

        // Act - User 2 tries to regenerate User 1's recovery codes
        var response = await Client.PostAsync($"/api/v1/auth/mfa/method/{methodId}/recovery-codes", null);

        // Assert - Should fail
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<string[]>>();
        result!.Success.Should().BeFalse("user should not be able to regenerate another user's recovery codes");
    }

    #endregion

    #region Push Device IDOR Tests

    [Fact]
    public async Task UpdateDeviceToken_OtherUsersDevice_ReturnsError()
    {
        // Arrange - User 1 registers a push device
        var user1Email = "idor-push-user1@example.com";
        var user1Token = await CreateUserAndGetTokenAsync(user1Email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);

        var registerResponse = await Client.PostAsJsonAsync("/api/v1/MfaPush/register-device", new RegisterPushDeviceRequest
        {
            DeviceId = $"device-{Guid.NewGuid():N}",
            DeviceName = "User 1 iPhone",
            Platform = "iOS",
            PushToken = $"token-{Guid.NewGuid():N}",
            PublicKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("user1-key"))
        });

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ServiceResponse<MfaPushDeviceDto>>();
        var deviceId = registerResult!.Data!.Id;

        // Arrange - User 2 logs in
        var user2Email = "idor-push-user2@example.com";
        var user2Token = await CreateUserAndGetTokenAsync(user2Email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2Token);

        // Act - User 2 tries to update User 1's device token
        var response = await Client.PutAsJsonAsync($"/api/v1/MfaPush/devices/{deviceId}/token", new UpdatePushTokenDto
        {
            NewToken = "malicious-token"
        });

        // Assert - Should fail
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeFalse("user should not be able to update another user's device token");
    }

    #endregion

    #region Admin Endpoint Authorization Tests

    [Fact]
    public async Task AdminGetUsers_WithoutPrivilege_ReturnsForbidden()
    {
        // Arrange - Regular user (no admin privileges)
        var userEmail = "regular-user@example.com";
        var userToken = await CreateUserAndGetTokenAsync(userEmail);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        // Act - Try to access admin endpoint
        var response = await Client.GetAsync("/api/v1/admin/User/GetAllUsers");

        // Assert - Should be forbidden (user exists but doesn't have privilege)
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminDeactivateUser_WithoutPrivilege_ReturnsForbidden()
    {
        // Arrange - Regular user
        var userEmail = "regular-deactivate@example.com";
        var userToken = await CreateUserAndGetTokenAsync(userEmail);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        var targetUserId = Guid.NewGuid();

        // Act - Try to deactivate a user without admin privilege
        var response = await Client.DeleteAsync($"/api/v1/admin/User/{targetUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MfaAdminStatistics_WithoutPrivilege_ReturnsForbidden()
    {
        // Arrange - Regular user
        var userEmail = "regular-stats@example.com";
        var userToken = await CreateUserAndGetTokenAsync(userEmail);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        // Act - Try to access MFA admin statistics
        var response = await Client.GetAsync("/api/v1/admin/MfaAdmin/statistics/system");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminCleanupUnverified_WithoutPrivilege_ReturnsForbidden()
    {
        // Arrange - Regular user
        var userEmail = "regular-cleanup@example.com";
        var userToken = await CreateUserAndGetTokenAsync(userEmail);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        // Act - Try to cleanup unverified MFA methods
        var response = await Client.DeleteAsync("/api/v1/admin/MfaAdmin/cleanup/unverified?olderThanHours=24");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region MFA Overview Privacy Tests

    [Fact]
    public async Task MfaOverview_ReturnsOnlyOwnMfaMethods()
    {
        // Arrange - User 1 sets up MFA
        var user1Email = "privacy-user1@example.com";
        var user1Token = await CreateUserAndGetTokenAsync(user1Email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);

        // User 1 sets up email MFA
        var user1MfaEmail = "privacy-mfa-user1@example.com";
        await Client.PostAsJsonAsync("/api/v1/auth/mfa/setup/email", new StartEmailMfaSetupDto
        {
            EmailAddress = user1MfaEmail
        });
        var code1 = TestEmailService.GetLastCodeForEmail(user1MfaEmail);
        await Client.PostAsJsonAsync("/api/v1/auth/mfa/verify/email", new VerifyMfaSetupDto { Code = code1! });

        // Arrange - User 2 logs in (no MFA setup)
        var user2Email = "privacy-user2@example.com";
        var user2Token = await CreateUserAndGetTokenAsync(user2Email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2Token);

        // Act - User 2 gets their MFA overview
        var response = await Client.GetAsync("/api/v1/auth/mfa/overview");

        // Assert - User 2 should see NO MFA methods (not User 1's)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<MfaOverviewDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.HasEnabledMfa.Should().BeFalse("user 2 has no MFA setup");
        result.Data.Methods.Should().BeEmpty("user 2 should not see user 1's MFA methods");
    }

    #endregion

    #region Cross-User Data Access Tests

    [Fact]
    public async Task GetDevices_ReturnsOnlyOwnDevices()
    {
        // Arrange - User 1 registers devices
        var user1Email = "devices-user1@example.com";
        var user1Token = await CreateUserAndGetTokenAsync(user1Email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);

        await Client.PostAsJsonAsync("/api/v1/MfaPush/register-device", new RegisterPushDeviceRequest
        {
            DeviceId = $"user1-device-{Guid.NewGuid():N}",
            DeviceName = "User 1 Device",
            Platform = "iOS",
            PushToken = $"token-{Guid.NewGuid():N}",
            PublicKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("key"))
        });

        // Arrange - User 2 logs in (no devices)
        var user2Email = "devices-user2@example.com";
        var user2Token = await CreateUserAndGetTokenAsync(user2Email);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2Token);

        // Act - User 2 gets their devices
        var response = await Client.GetAsync("/api/v1/MfaPush/devices");

        // Assert - User 2 should see NO devices
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<IEnumerable<MfaPushDeviceDto>>>();
        result!.Success.Should().BeTrue();
        result.Data.Should().BeEmpty("user 2 should not see user 1's devices");
    }

    #endregion

    #region Random GUID Access Tests

    [Fact]
    public async Task RemoveMfaMethod_RandomGuid_ReturnsError()
    {
        // Arrange
        var userEmail = "random-guid-mfa@example.com";
        var userToken = await CreateUserAndGetTokenAsync(userEmail);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        // Act - Try to delete a random MFA method ID
        var randomId = Guid.NewGuid();
        var response = await Client.DeleteAsync($"/api/v1/auth/mfa/method/{randomId}");

        // Assert - Should fail gracefully (not found or unauthorized, not server error)
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Data.Should().BeFalse("deleting non-existent MFA method should fail");
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task RemoveDevice_RandomGuid_ReturnsError()
    {
        // Arrange
        var userEmail = "random-guid-device@example.com";
        var userToken = await CreateUserAndGetTokenAsync(userEmail);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        // Act - Try to delete a random device ID
        var randomId = Guid.NewGuid();
        var response = await Client.DeleteAsync($"/api/v1/MfaPush/devices/{randomId}");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeFalse("deleting non-existent device should fail");
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
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