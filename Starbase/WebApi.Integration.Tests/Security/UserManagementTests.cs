using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs.Auth;
using Application.DTOs.Jwt;
using Application.DTOs.Users;
using Application.Models;
using Domain.Constants;
using Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests.Security;

public class UserManagementTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    #region Authorization Tests

    [Fact]
    public async Task GetAllUsers_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/admin/user/GetAllUsers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllUsers_AuthenticatedWithoutPrivilege_ReturnsForbidden()
    {
        // Arrange - Create user without any privileges
        var email = "no-privilege-user@example.com";
        var password = "Password123!";

        await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(password));

        var token = await LoginAndGetTokenAsync(email, password);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/admin/user/GetAllUsers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllUsers_AuthenticatedWithPrivilege_ReturnsSuccess()
    {
        // Arrange - Create user with View privilege
        var email = "admin-user@example.com";
        var password = "Password123!";

        await CreateUserWithPrivilegeAsync(email, password, PredefinedPrivileges.UserManagement.View);

        var token = await LoginAndGetTokenAsync(email, password);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/admin/user/GetAllUsers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<List<AppUserDto>>>();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateUser_WithoutDeactivatePrivilege_ReturnsForbidden()
    {
        // Arrange - Create user with View but not Deactivate privilege
        var adminEmail = "view-only-admin@example.com";
        var adminPassword = "Password123!";

        await CreateUserWithPrivilegeAsync(adminEmail, adminPassword, PredefinedPrivileges.UserManagement.View);

        // Create a target user to deactivate
        var targetUser = await CreateTestUserAsync(u => u
            .WithEmail("target-user@example.com")
            .WithPassword("Password123!"));

        var token = await LoginAndGetTokenAsync(adminEmail, adminPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.DeleteAsync($"/api/admin/user/{targetUser.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateUser_WithDeactivatePrivilege_ReturnsSuccess()
    {
        // Arrange
        var adminEmail = "deactivate-admin@example.com";
        var adminPassword = "Password123!";

        await CreateUserWithPrivilegeAsync(adminEmail, adminPassword, PredefinedPrivileges.UserManagement.Deactivate);

        var targetUser = await CreateTestUserAsync(u => u
            .WithEmail("user-to-deactivate@example.com")
            .WithPassword("Password123!"));

        var token = await LoginAndGetTokenAsync(adminEmail, adminPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.DeleteAsync($"/api/admin/user/{targetUser.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeTrue();

        // Verify user is deactivated
        await WithDbContextAsync(async db =>
        {
            var user = await db.AppUsers.FindAsync(targetUser.Id);
            user!.Active.Should().BeFalse();
        });
    }

    [Fact]
    public async Task CreateUser_WithoutCreatePrivilege_ReturnsForbidden()
    {
        // Arrange
        var adminEmail = "no-create-admin@example.com";
        var adminPassword = "Password123!";

        await CreateUserWithPrivilegeAsync(adminEmail, adminPassword, PredefinedPrivileges.UserManagement.View);

        var token = await LoginAndGetTokenAsync(adminEmail, adminPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newUser = new CreateNewUserDto
        {
            FirstName = "New",
            LastName = "User",
            Username = "newuser@example.com",
            Password = "SecurePassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/user", newUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_WithCreatePrivilege_ReturnsSuccess()
    {
        // Arrange
        var adminEmail = "create-admin@example.com";
        var adminPassword = "Password123!";

        await CreateUserWithPrivilegeAsync(adminEmail, adminPassword, PredefinedPrivileges.UserManagement.Create);

        var token = await LoginAndGetTokenAsync(adminEmail, adminPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newUser = new CreateNewUserDto
        {
            FirstName = "Created",
            LastName = "User",
            Username = "createduser@example.com",
            Password = "SecurePassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/user", newUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<AppUserDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.Username.Should().Be("createduser@example.com");

        // Verify user exists in database
        await WithDbContextAsync(async db =>
        {
            var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Username == "createduser@example.com");
            user.Should().NotBeNull();
            user!.FirstName.Should().Be("Created");
        });
    }

    #endregion

    #region Inactive User Tests

    [Fact]
    public async Task InactiveUser_WithExistingToken_CanStillAccessEndpoints_UntilTokenExpires()
    {
        // NOTE: This test documents current behavior - JWT tokens contain IsUserActive claim
        // at time of issuance. Deactivating a user doesn't invalidate their existing token.
        // For immediate lockout, consider: short token expiry, token blacklist, or DB check middleware.

        // Arrange - Create and login, then deactivate
        var email = "soon-inactive@example.com";
        var password = "Password123!";

        var user = await CreateUserWithPrivilegeAsync(email, password, PredefinedPrivileges.UserManagement.View);
        var token = await LoginAndGetTokenAsync(email, password);

        // Deactivate the user
        await WithDbContextAsync(async db =>
        {
            var dbUser = await db.AppUsers.FindAsync(user.Id);
            dbUser!.Deactivate();
            await db.SaveChangesAsync();
        });

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Try to access with token from now-inactive user
        var response = await Client.GetAsync("/api/admin/user/GetAllUsers");

        // Assert - Token still works because IsUserActive claim was set at login time
        // This is expected JWT behavior - user can access until token expires
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InactiveUser_CanLogin_ButTokenIndicatesInactiveStatus()
    {
        // Design note: Inactive users CAN login to support scenarios like viewing
        // historical data (e.g., timesheets). The [RequireActiveUser] attribute
        // on specific endpoints enforces access restrictions.

        // Arrange - Create user and deactivate them
        var email = "inactive-login@example.com";
        var password = "Password123!";

        var user = await CreateTestUserAsync(u => u
            .WithEmail(email)
            .WithPassword(password));

        await WithDbContextAsync(async db =>
        {
            var dbUser = await db.AppUsers.FindAsync(user.Id);
            dbUser!.Deactivate();
            await db.SaveChangesAsync();
        });

        // Act - Login as deactivated user
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });

        // Assert - Login succeeds but token indicates inactive status
        var result = await loginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        result!.Success.Should().BeTrue("inactive users can login for limited access scenarios");
        result.Data!.Token.Should().NotBeNullOrEmpty();

        // Verify the token contains IsUserActive=False claim
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Data.Token);
        var isUserActiveClaim = token.Claims.FirstOrDefault(c => c.Type == "IsUserActive");
        isUserActiveClaim.Should().NotBeNull();
        isUserActiveClaim!.Value.Should().Be("False", "token should indicate user is inactive");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task CreateUser_WithInvalidEmail_ReturnsValidationError()
    {
        // Arrange
        var adminEmail = "validation-admin@example.com";
        var adminPassword = "Password123!";

        await CreateUserWithPrivilegeAsync(adminEmail, adminPassword, PredefinedPrivileges.UserManagement.Create);

        var token = await LoginAndGetTokenAsync(adminEmail, adminPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newUser = new CreateNewUserDto
        {
            FirstName = "Test",
            LastName = "User",
            Username = "not-a-valid-email",
            Password = "SecurePassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/user", newUser);

        // Assert - ValidDto attribute returns 422 for validation failures
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUser_WithWeakPassword_ReturnsValidationError()
    {
        // Arrange
        var adminEmail = "weak-pass-admin@example.com";
        var adminPassword = "Password123!";

        await CreateUserWithPrivilegeAsync(adminEmail, adminPassword, PredefinedPrivileges.UserManagement.Create);

        var token = await LoginAndGetTokenAsync(adminEmail, adminPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newUser = new CreateNewUserDto
        {
            FirstName = "Test",
            LastName = "User",
            Username = "weakpassuser@example.com",
            Password = "weak"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/user", newUser);

        // Assert - API returns 200 with ServiceResponse indicating failure
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<AppUserDto>>();
        result!.Success.Should().BeFalse("weak password should fail validation");
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_ReturnsError()
    {
        // Arrange
        var adminEmail = "dupe-admin@example.com";
        var adminPassword = "Password123!";

        await CreateUserWithPrivilegeAsync(adminEmail, adminPassword, PredefinedPrivileges.UserManagement.Create);

        // Create an existing user in the same organization
        await CreateTestUserAsync(u => u.WithEmail("existing-in-org@example.com"));

        var token = await LoginAndGetTokenAsync(adminEmail, adminPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newUser = new CreateNewUserDto
        {
            FirstName = "Duplicate",
            LastName = "User",
            Username = "existing-in-org@example.com", // Already exists
            Password = "SecurePassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/user", newUser);

        // Assert - ValidDto attribute returns 422 for duplicate username
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    #endregion

    #region Helper Methods

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new UserLoginDto
        {
            Username = email,
            Password = password
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ServiceResponse<JwtResponseDto>>();
        return loginResult!.Data!.Token!;
    }

    private async Task<AppUser> CreateUserWithPrivilegeAsync(string email, string password, string privilegeName)
    {
        await EnsureTestOrganizationAsync();

        return await WithDbContextAsync(async db =>
        {
            // Create the privilege if it doesn't exist
            var privilege = await db.Set<Privilege>()
                .FirstOrDefaultAsync(p => p.Name == privilegeName);

            if (privilege is null)
            {
                privilege = new Privilege(privilegeName, $"Test privilege: {privilegeName}", true, false, false);
                db.Set<Privilege>().Add(privilege);
            }

            // Create a role with this privilege
            var roleName = $"TestRole_{Guid.NewGuid():N}";
            var role = new Role(roleName, null);
            role.AddPrivilege(privilege);
            db.Roles.Add(role);

            // Create the user with this role
            var builder = new TestUtils.EntityBuilders.AppUserBuilder()
                .WithEmail(email)
                .WithPassword(password);
            var user = builder.Build();
            user.AddRole(role);

            db.AppUsers.Add(user);
            await db.SaveChangesAsync();

            return user;
        });
    }

    #endregion
}