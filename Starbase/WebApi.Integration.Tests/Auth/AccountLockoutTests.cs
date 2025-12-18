using System.Net;
using System.Net.Http.Json;
using Application.DTOs.Auth;
using Application.DTOs.Jwt;
using Application.Models;
using Domain.Entities.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests.Auth;

public class AccountLockoutTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    // Default threshold from appsettings.json is 5 failed attempts
    private const int FailedAttemptThreshold = 5;

    [Fact]
    public async Task Login_AfterExceedingFailedAttempts_ReturnsAccountLocked()
    {
        // Arrange
        var email = "lockout-test@example.com";
        var correctPassword = "CorrectPassword123!";
        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(correctPassword)
            .WithForceResetPassword(false));

        // Act - Exceed the failed attempt threshold
        for (var i = 0; i < FailedAttemptThreshold; i++)
        {
            await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
            {
                Username = email,
                Password = "WrongPassword!"
            });
        }

        // Try to login with correct password after lockout
        var response = await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
        {
            Username = email,
            Password = correctPassword
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("locked", "account should be locked after too many failed attempts");
    }

    [Fact]
    public async Task Login_BelowFailedAttemptThreshold_AllowsLogin()
    {
        // Arrange
        var email = "below-threshold@example.com";
        var correctPassword = "CorrectPassword123!";
        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(correctPassword)
            .WithForceResetPassword(false));

        // Act - Make some failed attempts but stay below threshold
        for (var i = 0; i < FailedAttemptThreshold - 1; i++)
        {
            await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
            {
                Username = email,
                Password = "WrongPassword!"
            });
        }

        // Try to login with correct password
        var response = await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
        {
            Username = email,
            Password = correctPassword
        });

        // Assert - Should still be able to login
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeTrue("account should not be locked when below threshold");
        result.Data!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_SuccessfulLoginResetsFailedAttempts()
    {
        // Arrange
        var email = "reset-attempts@example.com";
        var correctPassword = "CorrectPassword123!";
        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(correctPassword)
            .WithForceResetPassword(false));

        // Make some failed attempts
        for (var i = 0; i < FailedAttemptThreshold - 2; i++)
        {
            await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
            {
                Username = email,
                Password = "WrongPassword!"
            });
        }

        // Successful login should reset counter
        var successResponse = await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
        {
            Username = email,
            Password = correctPassword
        });
        var successResult = await successResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        successResult!.Success.Should().BeTrue();

        // Act - Make more failed attempts (should start from 0 again)
        for (var i = 0; i < FailedAttemptThreshold - 2; i++)
        {
            await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
            {
                Username = email,
                Password = "WrongPassword!"
            });
        }

        // Should still be able to login since counter was reset
        var response = await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
        {
            Username = email,
            Password = correctPassword
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeTrue("failed attempt counter should have been reset after successful login");
    }

    [Fact]
    public async Task Login_NonExistentUser_ReturnsSameErrorAsWrongPassword()
    {
        // Security: Error message should not reveal whether the user exists
        // This prevents username enumeration attacks

        // Arrange - Create a real user for comparison
        var realEmail = "real-user-enum@example.com";
        var realPassword = "CorrectPassword123!";
        await CreateTestUserAsync(u => u
            .WithEmail(realEmail)
            .WithPassword(realPassword)
            .WithForceResetPassword(false));

        // Act - Try to login with non-existent user
        var nonExistentResponse = await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
        {
            Username = "nonexistent-user@example.com",
            Password = "SomePassword123!"
        });

        // Try to login with existing user but wrong password
        var wrongPasswordResponse = await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
        {
            Username = realEmail,
            Password = "WrongPassword123!"
        });

        // Assert - Both should return the same generic error message
        var nonExistentResult = await nonExistentResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        var wrongPasswordResult = await wrongPasswordResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();

        nonExistentResult!.Success.Should().BeFalse();
        wrongPasswordResult!.Success.Should().BeFalse();
        nonExistentResult.Message.Should().Be(wrongPasswordResult.Message,
            "error messages should be identical to prevent username enumeration");
    }

    [Fact]
    public async Task Login_LockedAccount_CreatesLockoutRecordInDatabase()
    {
        // Arrange
        var email = "lockout-record@example.com";
        var correctPassword = "CorrectPassword123!";
        var user = await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(correctPassword)
            .WithForceResetPassword(false));

        // Act - Exceed the failed attempt threshold to trigger lockout
        for (var i = 0; i < FailedAttemptThreshold; i++)
        {
            await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
            {
                Username = email,
                Password = "WrongPassword!"
            });
        }

        // Assert - Verify lockout record exists in database
        await WithDbContextAsync(async db =>
        {
            var lockout = await db.Set<AccountLockout>()
                .FirstOrDefaultAsync(l => l.UserId == user.Id);

            lockout.Should().NotBeNull("lockout record should be created");
            lockout!.IsLockedOut.Should().BeTrue("account should be marked as locked");
            lockout.FailedAttemptCount.Should().BeGreaterThanOrEqualTo(FailedAttemptThreshold);
            lockout.LockoutExpiresAt.Should().NotBeNull("lockout should have an expiration time");
            lockout.LockoutExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow, "lockout should expire in the future");
        });
    }

    [Fact]
    public async Task Login_AfterLockoutExpires_AllowsLoginAgain()
    {
        // Arrange
        var email = "lockout-expires@example.com";
        var correctPassword = "CorrectPassword123!";
        var user = await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(correctPassword)
            .WithForceResetPassword(false));

        // Trigger lockout
        for (var i = 0; i < FailedAttemptThreshold; i++)
        {
            await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
            {
                Username = email,
                Password = "WrongPassword!"
            });
        }

        // Verify account is locked
        var lockedResponse = await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
        {
            Username = email,
            Password = correctPassword
        });
        var lockedResult = await lockedResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        lockedResult!.Success.Should().BeFalse("account should be locked");

        // Act - Manually expire the lockout in the database (simulating time passing)
        await WithDbContextAsync(async db =>
        {
            var lockout = await db.Set<AccountLockout>()
                .FirstOrDefaultAsync(l => l.UserId == user.Id);

            // Set expiration to the past
            db.Entry(lockout!).Property("LockoutExpiresAt").CurrentValue = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        });

        // Try to login after lockout expired
        var response = await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
        {
            Username = email,
            Password = correctPassword
        });

        // Assert - Should be able to login now
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeTrue("account should be unlocked after lockout expires");
        result.Data!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WhileLocked_DoesNotExtendLockout()
    {
        // Arrange
        var email = "no-extend-lockout@example.com";
        var correctPassword = "CorrectPassword123!";
        var user = await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(correctPassword)
            .WithForceResetPassword(false));

        // Trigger lockout
        for (var i = 0; i < FailedAttemptThreshold; i++)
        {
            await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
            {
                Username = email,
                Password = "WrongPassword!"
            });
        }

        // Get initial lockout expiration
        DateTimeOffset? initialExpiration = null;
        await WithDbContextAsync(async db =>
        {
            var lockout = await db.Set<AccountLockout>()
                .FirstOrDefaultAsync(l => l.UserId == user.Id);
            initialExpiration = lockout!.LockoutExpiresAt;
        });

        // Act - Make more failed attempts while locked
        for (var i = 0; i < 3; i++)
        {
            await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
            {
                Username = email,
                Password = "WrongPassword!"
            });
        }

        // Assert - Lockout expiration should not have changed
        await WithDbContextAsync(async db =>
        {
            var lockout = await db.Set<AccountLockout>()
                .FirstOrDefaultAsync(l => l.UserId == user.Id);

            lockout!.LockoutExpiresAt.Should().Be(initialExpiration,
                "additional failed attempts while locked should not extend lockout duration");
        });
    }

    [Fact]
    public async Task Login_TracksLoginAttempts()
    {
        // Arrange
        var email = "track-attempts@example.com";
        var correctPassword = "CorrectPassword123!";
        var user = await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(correctPassword)
            .WithForceResetPassword(false));

        // Act - Make some failed attempts
        for (var i = 0; i < 3; i++)
        {
            await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
            {
                Username = email,
                Password = "WrongPassword!"
            });
        }

        // Make a successful attempt
        await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
        {
            Username = email,
            Password = correctPassword
        });

        // Assert - Verify login attempts are tracked in database
        await WithDbContextAsync(async db =>
        {
            var attempts = await db.Set<LoginAttempt>()
                .Where(a => a.UserId == user.Id)
                .ToListAsync();

            attempts.Should().HaveCount(4, "all login attempts should be tracked");
            attempts.Count(a => !a.IsSuccessful).Should().Be(3, "3 failed attempts");
            attempts.Count(a => a.IsSuccessful).Should().Be(1, "1 successful attempt");
        });
    }
}