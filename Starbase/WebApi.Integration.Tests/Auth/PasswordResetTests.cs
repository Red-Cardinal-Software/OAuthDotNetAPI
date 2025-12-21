using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs.Auth;
using Application.DTOs.Jwt;
using Application.Models;
using Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests.Auth;

public class PasswordResetTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    #region Request Password Reset Tests

    [Fact]
    public async Task RequestPasswordReset_ExistingUser_ReturnsSuccessWithMessage()
    {
        // Arrange
        var email = "reset-request@example.com";
        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword("OldPassword123!"));

        // Act
        var response = await Client.PostAsync($"/api/v1/auth/ResetPassword/{email}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Message.Should().NotBeNullOrEmpty();

        // Verify a password reset token was created in the database
        await WithDbContextAsync(async db =>
        {
            var user = await db.AppUsers.FirstAsync(u => u.Username == email);
            var tokens = await db.Set<PasswordResetToken>()
                .Where(t => t.AppUserId == user.Id)
                .ToListAsync();
            tokens.Should().NotBeEmpty("a reset token should be created for the user");
        });
    }

    [Fact]
    public async Task RequestPasswordReset_NonExistentUser_ReturnsSuccessToPreventEnumeration()
    {
        // Arrange - No user with this email exists
        var email = "nonexistent-user@example.com";

        // Act
        var response = await Client.PostAsync($"/api/v1/auth/ResetPassword/{email}", null);

        // Assert
        // Should return success to prevent username enumeration attacks
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue("should return success even for non-existent user to prevent enumeration");
    }

    [Fact]
    public async Task RequestPasswordReset_MultipleRequests_CreatesMultipleTokens()
    {
        // Arrange
        var email = "multi-reset@example.com";
        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword("OldPassword123!"));

        // Act - Request reset twice
        await Client.PostAsync($"/api/v1/auth/ResetPassword/{email}", null);
        await Client.PostAsync($"/api/v1/auth/ResetPassword/{email}", null);

        // Assert - Multiple tokens should exist (until one is claimed)
        await WithDbContextAsync(async db =>
        {
            var user = await db.AppUsers.FirstAsync(u => u.Username == email);
            var tokens = await db.Set<PasswordResetToken>()
                .Where(t => t.AppUserId == user.Id && t.ClaimedDate == null)
                .ToListAsync();
            tokens.Count.Should().BeGreaterThan(1, "multiple unclaimed tokens should exist");
        });
    }

    #endregion

    #region Apply Password Reset Tests

    [Fact]
    public async Task ApplyPasswordReset_ValidToken_ResetsPasswordSuccessfully()
    {
        // Arrange
        var email = "apply-reset@example.com";
        var oldPassword = "OldPassword123!";
        var newPassword = "NewSecurePassword123!";

        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(oldPassword)
            .WithForceResetPassword(false));

        // Request password reset through API (creates token in proper context)
        await Client.PostAsync($"/api/v1/auth/ResetPassword/{email}", null);

        // Get the token from the database
        var tokenId = await GetPasswordResetTokenIdAsync(email);

        var resetRequest = new PasswordResetSubmissionDto
        {
            Token = tokenId.ToString(),
            Password = newPassword
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/ResetUserPassword", resetRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeTrue();

        // Verify user can log in with new password
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = newPassword
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        loginResult!.Success.Should().BeTrue("user should be able to log in with new password");

        // Verify old password no longer works
        var oldLoginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = oldPassword
        });

        var oldLoginResult = await oldLoginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        oldLoginResult!.Success.Should().BeFalse("old password should no longer work");
    }

    [Fact]
    public async Task ApplyPasswordReset_InvalidToken_ReturnsFailure()
    {
        // Arrange
        var resetRequest = new PasswordResetSubmissionDto
        {
            Token = Guid.NewGuid().ToString(), // Random, non-existent token
            Password = "NewSecurePassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/ResetUserPassword", resetRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeFalse("invalid token should fail");
        result.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ApplyPasswordReset_MalformedToken_ReturnsFailure()
    {
        // Arrange
        var resetRequest = new PasswordResetSubmissionDto
        {
            Token = "not-a-valid-guid-format",
            Password = "NewSecurePassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/ResetUserPassword", resetRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeFalse("malformed token should fail");
    }

    [Fact]
    public async Task ApplyPasswordReset_AlreadyClaimedToken_ReturnsFailure()
    {
        // Arrange
        var email = "claimed-token@example.com";
        var newPassword = "NewSecurePassword123!";

        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword("OldPassword123!")
            .WithForceResetPassword(false));

        // Request password reset through API
        await Client.PostAsync($"/api/v1/auth/ResetPassword/{email}", null);

        // Get the token from the database
        var tokenId = await GetPasswordResetTokenIdAsync(email);

        var firstResetRequest = new PasswordResetSubmissionDto
        {
            Token = tokenId.ToString(),
            Password = newPassword
        };

        // First reset should succeed
        var firstResponse = await Client.PostAsJsonAsync("/api/v1/auth/ResetUserPassword", firstResetRequest);
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        firstResult!.Success.Should().BeTrue("first reset should succeed");

        // Act - Try to use the same token again
        var secondResetRequest = new PasswordResetSubmissionDto
        {
            Token = tokenId.ToString(),
            Password = "AnotherPassword123!"
        };

        var secondResponse = await Client.PostAsJsonAsync("/api/v1/auth/ResetUserPassword", secondResetRequest);

        // Assert
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        secondResult!.Success.Should().BeFalse("already claimed token should fail");
    }

    [Fact]
    public async Task ApplyPasswordReset_WeakPassword_ReturnsValidationFailure()
    {
        // Arrange
        var email = "weak-password@example.com";

        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword("StrongPassword123!"));

        // Request password reset through API
        await Client.PostAsync($"/api/v1/auth/ResetPassword/{email}", null);

        var tokenId = await GetPasswordResetTokenIdAsync(email);

        var resetRequest = new PasswordResetSubmissionDto
        {
            Token = tokenId.ToString(),
            Password = "weak" // Too simple
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/ResetUserPassword", resetRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeFalse("weak password should fail validation");
    }

    [Fact]
    public async Task ApplyPasswordReset_ClearsOtherUnclaimedTokens()
    {
        // Arrange
        var email = "multi-token-claim@example.com";

        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword("OldPassword123!")
            .WithForceResetPassword(false));

        // Create multiple reset tokens through the API
        await Client.PostAsync($"/api/v1/auth/ResetPassword/{email}", null);
        await Client.PostAsync($"/api/v1/auth/ResetPassword/{email}", null);
        await Client.PostAsync($"/api/v1/auth/ResetPassword/{email}", null);

        // Get the first token
        var tokenId1 = await GetPasswordResetTokenIdAsync(email);

        var resetRequest = new PasswordResetSubmissionDto
        {
            Token = tokenId1.ToString(),
            Password = "NewSecurePassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/ResetUserPassword", resetRequest);

        // Assert
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeTrue();

        // All tokens should now be claimed
        await WithDbContextAsync(async db =>
        {
            var user = await db.AppUsers.FirstAsync(u => u.Username == email);
            var unclaimedTokens = await db.Set<PasswordResetToken>()
                .Where(t => t.AppUserId == user.Id && t.ClaimedDate == null)
                .ToListAsync();
            unclaimedTokens.Should().BeEmpty("all tokens should be claimed after one is used");
        });
    }

    #endregion

    #region Force Password Reset Tests

    [Fact]
    public async Task ForcePasswordReset_AuthenticatedWithFlag_ResetsSuccessfully()
    {
        // Arrange
        var email = "force-reset@example.com";
        var oldPassword = "TempPassword123!";
        var newPassword = "NewSecurePassword456!";

        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(oldPassword)
            .WithForceResetPassword(true)); // Flag is set

        // Login to get token (login should work even with force reset flag)
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = oldPassword
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        loginResult!.Success.Should().BeTrue();
        loginResult.Data!.ForceReset.Should().BeTrue("should indicate force reset is required");

        var token = loginResult.Data.Token!;
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/ForcePasswordReset", newPassword);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeTrue();

        // Clear auth and verify new password works
        Client.DefaultRequestHeaders.Authorization = null;

        var newLoginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = newPassword
        });

        var newLoginResult = await newLoginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        newLoginResult!.Success.Should().BeTrue("new password should work after force reset");
        newLoginResult.Data!.ForceReset.Should().BeFalse("force reset flag should be cleared");
    }

    [Fact]
    public async Task ForcePasswordReset_WithoutFlag_ReturnsUnauthorized()
    {
        // Arrange
        var email = "no-force-flag@example.com";
        var password = "CurrentPassword123!";

        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(password)
            .WithForceResetPassword(false)); // Flag is NOT set

        var token = await LoginAndGetTokenAsync(email, password);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/ForcePasswordReset", "NewPassword123!");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeFalse("should fail when force reset flag is not set");
    }

    [Fact]
    public async Task ForcePasswordReset_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange - No authentication header set

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/ForcePasswordReset", "NewPassword123!");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ForcePasswordReset_SameAsCurrentPassword_ReturnsFailure()
    {
        // Arrange
        var email = "same-password@example.com";
        var currentPassword = "CurrentPassword123!";

        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(currentPassword)
            .WithForceResetPassword(true));

        var token = await LoginAndGetTokenAsync(email, currentPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Try to reset to the same password
        var response = await Client.PostAsJsonAsync("/api/v1/auth/ForcePasswordReset", currentPassword);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeFalse("should fail when new password is same as current");
    }

    [Fact]
    public async Task ForcePasswordReset_WeakPassword_ReturnsValidationFailure()
    {
        // Arrange
        var email = "weak-force-reset@example.com";
        var currentPassword = "CurrentPassword123!";

        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(currentPassword)
            .WithForceResetPassword(true));

        var token = await LoginAndGetTokenAsync(email, currentPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/ForcePasswordReset", "weak");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeFalse("weak password should fail validation");
    }

    #endregion

    #region Helper Methods

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        return loginResult!.Data!.Token!;
    }

    private async Task<Guid> GetPasswordResetTokenIdAsync(string email)
    {
        return await WithDbContextAsync(async db =>
        {
            var user = await db.AppUsers.FirstAsync(u => u.Username == email);
            var token = await db.Set<PasswordResetToken>()
                .Where(t => t.AppUserId == user.Id && t.ClaimedDate == null)
                .OrderByDescending(t => t.Id)
                .FirstAsync();
            return token.Id;
        });
    }

    #endregion
}