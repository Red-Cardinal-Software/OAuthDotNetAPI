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
/// Integration tests for Push MFA device management endpoints.
/// </summary>
public class MfaPushTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    #region Authorization Tests

    [Fact]
    public async Task RegisterDevice_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/MfaPush/register-device", new RegisterPushDeviceRequest
        {
            DeviceId = "test-device-123",
            DeviceName = "Test iPhone",
            Platform = "iOS",
            PushToken = "test-push-token",
            PublicKey = "test-public-key"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDevices_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/MfaPush/devices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveDevice_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.DeleteAsync($"/api/v1/MfaPush/devices/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Device Registration Tests

    [Fact]
    public async Task RegisterDevice_Authenticated_ReturnsSuccess()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("push-register@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new RegisterPushDeviceRequest
        {
            DeviceId = $"device-{Guid.NewGuid():N}",
            DeviceName = "Test iPhone 15",
            Platform = "iOS",
            PushToken = $"push-token-{Guid.NewGuid():N}",
            PublicKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test-public-key"))
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/MfaPush/register-device", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<MfaPushDeviceDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.DeviceName.Should().Be("Test iPhone 15");
        result.Data.Platform.Should().Be("iOS");
        result.Data.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterDevice_SameDeviceTwice_UpdatesExistingDevice()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("push-update@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var deviceId = $"device-{Guid.NewGuid():N}";
        var firstRequest = new RegisterPushDeviceRequest
        {
            DeviceId = deviceId,
            DeviceName = "Test iPhone",
            Platform = "iOS",
            PushToken = "first-push-token",
            PublicKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test-public-key"))
        };

        // Register first time
        await Client.PostAsJsonAsync("/api/v1/MfaPush/register-device", firstRequest);

        // Register same device with new token
        var secondRequest = new RegisterPushDeviceRequest
        {
            DeviceId = deviceId,
            DeviceName = "Test iPhone",
            Platform = "iOS",
            PushToken = "second-push-token",
            PublicKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test-public-key"))
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/MfaPush/register-device", secondRequest);

        // Assert - Should succeed and update the token
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<MfaPushDeviceDto>>();
        result!.Success.Should().BeTrue();
        result.Message!.ToLower().Should().Contain("updated");
    }

    [Fact]
    public async Task RegisterDevice_WithInvalidToken_ReturnsError()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("push-invalid@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new RegisterPushDeviceRequest
        {
            DeviceId = $"device-{Guid.NewGuid():N}",
            DeviceName = "Test Device",
            Platform = "iOS",
            PushToken = "", // Empty token - invalid
            PublicKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test-public-key"))
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/MfaPush/register-device", request);

        // Assert
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<MfaPushDeviceDto>>();
        result!.Success.Should().BeFalse("empty push token should be invalid");
    }

    #endregion

    #region Get Devices Tests

    [Fact]
    public async Task GetDevices_NoDevices_ReturnsEmptyList()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("push-nodevices@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/v1/MfaPush/devices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<IEnumerable<MfaPushDeviceDto>>>();
        result!.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDevices_WithRegisteredDevices_ReturnsDeviceList()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("push-getdevices@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Register two devices
        await Client.PostAsJsonAsync("/api/v1/MfaPush/register-device", new RegisterPushDeviceRequest
        {
            DeviceId = $"device-1-{Guid.NewGuid():N}",
            DeviceName = "iPhone 15 Pro",
            Platform = "iOS",
            PushToken = $"token-1-{Guid.NewGuid():N}",
            PublicKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("key-1"))
        });

        await Client.PostAsJsonAsync("/api/v1/MfaPush/register-device", new RegisterPushDeviceRequest
        {
            DeviceId = $"device-2-{Guid.NewGuid():N}",
            DeviceName = "Pixel 8",
            Platform = "Android",
            PushToken = $"token-2-{Guid.NewGuid():N}",
            PublicKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("key-2"))
        });

        // Act
        var response = await Client.GetAsync("/api/v1/MfaPush/devices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<IEnumerable<MfaPushDeviceDto>>>();
        result!.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data.Should().Contain(d => d.DeviceName == "iPhone 15 Pro");
        result.Data.Should().Contain(d => d.DeviceName == "Pixel 8");
    }

    #endregion

    #region Remove Device Tests

    [Fact]
    public async Task RemoveDevice_ExistingDevice_ReturnsSuccess()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("push-remove@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Register a device first
        var registerResponse = await Client.PostAsJsonAsync("/api/v1/MfaPush/register-device", new RegisterPushDeviceRequest
        {
            DeviceId = $"device-to-remove-{Guid.NewGuid():N}",
            DeviceName = "Device to Remove",
            Platform = "iOS",
            PushToken = $"token-{Guid.NewGuid():N}",
            PublicKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("key"))
        });

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ServiceResponse<MfaPushDeviceDto>>();
        var deviceId = registerResult!.Data!.Id;

        // Act
        var response = await Client.DeleteAsync($"/api/v1/MfaPush/devices/{deviceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeTrue();

        // Verify device is deactivated (soft delete - device remains in list but IsActive=false)
        var devicesResponse = await Client.GetAsync("/api/v1/MfaPush/devices");
        var devicesResult = await devicesResponse.Content.ReadFromJsonAsync<ServiceResponse<IEnumerable<MfaPushDeviceDto>>>();
        var removedDevice = devicesResult!.Data!.FirstOrDefault(d => d.Id == deviceId);
        removedDevice.Should().NotBeNull("device should still exist in list as soft-deleted");
        removedDevice!.IsActive.Should().BeFalse("device should be marked as inactive after removal");
    }

    [Fact]
    public async Task RemoveDevice_NonExistentDevice_ReturnsError()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("push-remove-notfound@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.DeleteAsync($"/api/v1/MfaPush/devices/{Guid.NewGuid()}");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeFalse("removing non-existent device should fail");
    }

    [Fact]
    public async Task RemoveDevice_OtherUsersDevice_ReturnsError()
    {
        // Arrange - User 1 registers a device
        var token1 = await CreateUserAndGetTokenAsync("push-user1@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);

        var registerResponse = await Client.PostAsJsonAsync("/api/v1/MfaPush/register-device", new RegisterPushDeviceRequest
        {
            DeviceId = $"user1-device-{Guid.NewGuid():N}",
            DeviceName = "User 1 Device",
            Platform = "iOS",
            PushToken = $"token-{Guid.NewGuid():N}",
            PublicKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("key"))
        });

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ServiceResponse<MfaPushDeviceDto>>();
        var deviceId = registerResult!.Data!.Id;

        // Arrange - User 2 tries to remove User 1's device
        var token2 = await CreateUserAndGetTokenAsync("push-user2@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);

        // Act
        var response = await Client.DeleteAsync($"/api/v1/MfaPush/devices/{deviceId}");

        // Assert - Should fail because device belongs to another user
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeFalse("user should not be able to remove another user's device");
    }

    #endregion

    #region Update Token Tests

    [Fact]
    public async Task UpdateDeviceToken_ExistingDevice_ReturnsSuccess()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("push-update-token@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Register a device first
        var registerResponse = await Client.PostAsJsonAsync("/api/v1/MfaPush/register-device", new RegisterPushDeviceRequest
        {
            DeviceId = $"device-update-token-{Guid.NewGuid():N}",
            DeviceName = "Token Update Device",
            Platform = "iOS",
            PushToken = $"old-token-{Guid.NewGuid():N}",
            PublicKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("key"))
        });

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ServiceResponse<MfaPushDeviceDto>>();
        var deviceId = registerResult!.Data!.Id;

        // Act
        var response = await Client.PutAsJsonAsync($"/api/v1/MfaPush/devices/{deviceId}/token", new UpdatePushTokenDto
        {
            NewToken = $"new-token-{Guid.NewGuid():N}"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateDeviceToken_NonExistentDevice_ReturnsError()
    {
        // Arrange
        var token = await CreateUserAndGetTokenAsync("push-update-notfound@example.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var nonExistentDeviceId = Guid.NewGuid();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/v1/MfaPush/devices/{nonExistentDeviceId}/token", new UpdatePushTokenDto
        {
            NewToken = "new-token-for-nonexistent"
        });

        // Assert - Check HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK, "API returns 200 with ServiceResponse wrapper");
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse($"updating token for non-existent device {nonExistentDeviceId} should fail. Message: {result.Message}");
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