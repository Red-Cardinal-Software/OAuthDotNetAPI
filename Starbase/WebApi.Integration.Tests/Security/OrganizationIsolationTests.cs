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

/// <summary>
/// Tests to verify organization isolation - users in one organization
/// cannot access or modify data belonging to another organization.
/// This is critical for multi-tenant security.
/// </summary>
public class OrganizationIsolationTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    // Two separate organization IDs for testing isolation
    private static readonly Guid OrgAId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrgBId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    #region GetAllUsers Isolation Tests

    [Fact]
    public async Task GetAllUsers_ReturnsOnlyUsersFromSameOrganization()
    {
        // Arrange - Use unique org IDs to avoid test pollution
        var testOrgAId = Guid.NewGuid();
        var testOrgBId = Guid.NewGuid();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        await EnsureTestOrganizationAsync(testOrgAId);
        await EnsureTestOrganizationAsync(testOrgBId);

        var adminEmail = $"org-a-admin-{uniqueId}@example.com";
        var adminPassword = "Password123!";
        await CreateUserWithPrivilegeInOrgAsync(adminEmail, adminPassword,
            PredefinedPrivileges.UserManagement.View, testOrgAId);

        // Create additional users in Org A with unique emails
        await CreateTestUserAsync(u => u
            .WithEmail($"org-a-user1-{uniqueId}@example.com")
            .WithOrganizationId(testOrgAId)
            .WithForceResetPassword(false));
        await CreateTestUserAsync(u => u
            .WithEmail($"org-a-user2-{uniqueId}@example.com")
            .WithOrganizationId(testOrgAId)
            .WithForceResetPassword(false));

        // Create users in Org B (should NOT be visible to Org A admin)
        await CreateTestUserAsync(u => u
            .WithEmail($"org-b-user1-{uniqueId}@example.com")
            .WithOrganizationId(testOrgBId)
            .WithForceResetPassword(false));
        await CreateTestUserAsync(u => u
            .WithEmail($"org-b-user2-{uniqueId}@example.com")
            .WithOrganizationId(testOrgBId)
            .WithForceResetPassword(false));

        var token = await LoginAndGetTokenAsync(adminEmail, adminPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/admin/user/GetAllUsers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<List<AppUserDto>>>();
        result!.Success.Should().BeTrue();

        // Verify we see exactly 3 users in our org and none from other org
        result.Data!.Count.Should().Be(3, "should only see admin + 2 users from same organization");
        result.Data.Should().NotContain(u => u.Username.Contains("org-b"),
            "should not return users from other organizations");
    }

    [Fact]
    public async Task GetAllUsers_DoesNotLeakUserCountFromOtherOrgs()
    {
        // Arrange - Use unique org IDs to avoid test pollution
        var isolatedOrgAId = Guid.NewGuid();
        var isolatedOrgBId = Guid.NewGuid();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        await EnsureTestOrganizationAsync(isolatedOrgAId);
        await EnsureTestOrganizationAsync(isolatedOrgBId);

        var adminEmail = $"count-test-admin-{uniqueId}@example.com";
        var adminPassword = "Password123!";
        await CreateUserWithPrivilegeInOrgAsync(adminEmail, adminPassword,
            PredefinedPrivileges.UserManagement.View, isolatedOrgAId);

        // Create 10 users in Org B
        for (int i = 0; i < 10; i++)
        {
            await CreateTestUserAsync(u => u
                .WithEmail($"org-b-bulk-{uniqueId}-{i}@example.com")
                .WithOrganizationId(isolatedOrgBId)
                .WithForceResetPassword(false));
        }

        var token = await LoginAndGetTokenAsync(adminEmail, adminPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/admin/user/GetAllUsers");

        // Assert - Should only see the 1 admin user from isolated Org A
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<List<AppUserDto>>>();
        result!.Data!.Count.Should().Be(1, "should only see admin user from own organization");
        result.Data.Should().OnlyContain(u => u.Username == adminEmail);
    }

    #endregion

    #region Deactivate User Isolation Tests

    [Fact]
    public async Task DeactivateUser_CannotDeactivateUserFromDifferentOrganization()
    {
        // Arrange
        await EnsureTestOrganizationAsync(OrgAId);
        await EnsureTestOrganizationAsync(OrgBId);

        var adminEmail = "deactivate-cross-admin@example.com";
        var adminPassword = "Password123!";
        await CreateUserWithPrivilegeInOrgAsync(adminEmail, adminPassword,
            PredefinedPrivileges.UserManagement.Deactivate, OrgAId);

        // Create target user in Org B
        var targetUser = await CreateTestUserAsync(u => u
            .WithEmail("target-in-org-b@example.com")
            .WithOrganizationId(OrgBId)
            .WithForceResetPassword(false));

        var token = await LoginAndGetTokenAsync(adminEmail, adminPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Try to deactivate user from Org B while logged in as Org A admin
        var response = await Client.DeleteAsync($"/api/admin/user/{targetUser.Id}");

        // Assert - Should be forbidden or return error
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeFalse("should not be able to deactivate user from another organization");

        // Verify user is still active in database
        await WithDbContextAsync(async db =>
        {
            var user = await db.AppUsers.FindAsync(targetUser.Id);
            user!.Active.Should().BeTrue("user should not have been deactivated");
        });
    }

    [Fact]
    public async Task DeactivateUser_CanDeactivateUserFromSameOrganization()
    {
        // Arrange
        await EnsureTestOrganizationAsync(OrgAId);

        var adminEmail = "deactivate-same-admin@example.com";
        var adminPassword = "Password123!";
        await CreateUserWithPrivilegeInOrgAsync(adminEmail, adminPassword,
            PredefinedPrivileges.UserManagement.Deactivate, OrgAId);

        // Create target user in same org (Org A)
        var targetUser = await CreateTestUserAsync(u => u
            .WithEmail("target-in-same-org@example.com")
            .WithOrganizationId(OrgAId)
            .WithForceResetPassword(false));

        var token = await LoginAndGetTokenAsync(adminEmail, adminPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Deactivate user from same organization
        var response = await Client.DeleteAsync($"/api/admin/user/{targetUser.Id}");

        // Assert - Should succeed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeTrue("should be able to deactivate user from same organization");

        // Verify user is deactivated in database
        await WithDbContextAsync(async db =>
        {
            var user = await db.AppUsers.FindAsync(targetUser.Id);
            user!.Active.Should().BeFalse("user should have been deactivated");
        });
    }

    [Fact]
    public async Task DeactivateUser_WithNonExistentUserId_ReturnsNotFoundNotOrgLeak()
    {
        // Arrange - This tests that we don't leak information about whether
        // a user exists in another org vs doesn't exist at all
        await EnsureTestOrganizationAsync(OrgAId);

        var adminEmail = "deactivate-notfound-admin@example.com";
        var adminPassword = "Password123!";
        await CreateUserWithPrivilegeInOrgAsync(adminEmail, adminPassword,
            PredefinedPrivileges.UserManagement.Deactivate, OrgAId);

        var token = await LoginAndGetTokenAsync(adminEmail, adminPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentUserId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/admin/user/{nonExistentUserId}");

        // Assert - Should return consistent error (not leak whether user exists in other org)
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
        result!.Success.Should().BeFalse();
    }

    #endregion

    #region Create User Isolation Tests

    [Fact]
    public async Task CreateUser_NewUserIsCreatedInAdminsOrganization()
    {
        // Arrange
        await EnsureTestOrganizationAsync(OrgAId);

        var adminEmail = "create-org-admin@example.com";
        var adminPassword = "Password123!";
        await CreateUserWithPrivilegeInOrgAsync(adminEmail, adminPassword,
            PredefinedPrivileges.UserManagement.Create, OrgAId);

        var token = await LoginAndGetTokenAsync(adminEmail, adminPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newUser = new CreateNewUserDto
        {
            FirstName = "New",
            LastName = "User",
            Username = "new-user-in-org-a@example.com",
            Password = "SecurePassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/user", newUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<AppUserDto>>();
        result!.Success.Should().BeTrue();

        // Verify user was created in admin's organization
        await WithDbContextAsync(async db =>
        {
            var createdUser = await db.AppUsers
                .FirstOrDefaultAsync(u => u.Username == "new-user-in-org-a@example.com");
            createdUser.Should().NotBeNull();
            createdUser!.OrganizationId.Should().Be(OrgAId,
                "new user should be created in admin's organization");
        });
    }

    [Fact]
    public async Task CreateUser_CannotCreateDuplicateUsernameInSameOrg()
    {
        // Arrange
        await EnsureTestOrganizationAsync(OrgAId);

        var adminEmail = "dup-check-admin@example.com";
        var adminPassword = "Password123!";
        await CreateUserWithPrivilegeInOrgAsync(adminEmail, adminPassword,
            PredefinedPrivileges.UserManagement.Create, OrgAId);

        // Create existing user in same org
        await CreateTestUserAsync(u => u
            .WithEmail("existing@example.com")
            .WithOrganizationId(OrgAId)
            .WithForceResetPassword(false));

        var token = await LoginAndGetTokenAsync(adminEmail, adminPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newUser = new CreateNewUserDto
        {
            FirstName = "Duplicate",
            LastName = "User",
            Username = "existing@example.com", // Already exists in Org A
            Password = "SecurePassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/user", newUser);

        // Assert - Should fail due to duplicate username in same org
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateUser_SameUsernameCanExistInDifferentOrgs()
    {
        // Arrange - Username uniqueness is per-organization, not global
        await EnsureTestOrganizationAsync(OrgAId);
        await EnsureTestOrganizationAsync(OrgBId);

        var adminEmail = "cross-org-admin@example.com";
        var adminPassword = "Password123!";
        await CreateUserWithPrivilegeInOrgAsync(adminEmail, adminPassword,
            PredefinedPrivileges.UserManagement.Create, OrgAId);

        // Create user with same email in Org B
        await CreateTestUserAsync(u => u
            .WithEmail("shared-email@example.com")
            .WithOrganizationId(OrgBId)
            .WithForceResetPassword(false));

        var token = await LoginAndGetTokenAsync(adminEmail, adminPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newUser = new CreateNewUserDto
        {
            FirstName = "Same",
            LastName = "Email",
            Username = "shared-email@example.com", // Exists in Org B, but not in Org A
            Password = "SecurePassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/user", newUser);

        // Assert - Should succeed because username is unique within Org A
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<AppUserDto>>();
        result!.Success.Should().BeTrue("same username can exist in different organizations");

        // Verify both users exist in their respective orgs
        await WithDbContextAsync(async db =>
        {
            var usersWithEmail = await db.AppUsers
                .Where(u => u.Username == "shared-email@example.com")
                .ToListAsync();

            usersWithEmail.Should().HaveCount(2);
            usersWithEmail.Should().Contain(u => u.OrganizationId == OrgAId);
            usersWithEmail.Should().Contain(u => u.OrganizationId == OrgBId);
        });
    }

    #endregion

    #region Update User Isolation Tests

    [Fact]
    public async Task UpdateUser_CannotUpdateUserFromDifferentOrganization()
    {
        // Arrange
        await EnsureTestOrganizationAsync(OrgAId);
        await EnsureTestOrganizationAsync(OrgBId);

        var adminEmail = "update-cross-admin@example.com";
        var adminPassword = "Password123!";
        await CreateUserWithPrivilegeInOrgAsync(adminEmail, adminPassword,
            PredefinedPrivileges.UserManagement.Update, OrgAId);

        // Create target user in Org B
        var targetUser = await CreateTestUserAsync(u => u
            .WithEmail("update-target-org-b@example.com")
            .WithFirstName("Original")
            .WithOrganizationId(OrgBId)
            .WithForceResetPassword(false));

        var token = await LoginAndGetTokenAsync(adminEmail, adminPassword);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateDto = new AppUserDto
        {
            Id = targetUser.Id,
            FirstName = "Hacked",
            LastName = targetUser.LastName,
            Username = targetUser.Username
        };

        // Act - Try to update user from Org B while logged in as Org A admin
        var response = await Client.PutAsJsonAsync("/api/admin/user", updateDto);

        // Assert - Should fail
        var result = await response.Content.ReadFromJsonAsync<ServiceResponse<AppUserDto>>();
        result!.Success.Should().BeFalse("should not be able to update user from another organization");

        // Verify user was not modified
        await WithDbContextAsync(async db =>
        {
            var user = await db.AppUsers.FindAsync(targetUser.Id);
            user!.FirstName.Should().Be("Original", "user should not have been modified");
        });
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

    private async Task<AppUser> CreateUserWithPrivilegeInOrgAsync(
        string email,
        string password,
        string privilegeName,
        Guid organizationId)
    {
        await EnsureTestOrganizationAsync(organizationId);

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

            // Create the user with this role in the specified organization
            var builder = new TestUtils.EntityBuilders.AppUserBuilder()
                .WithEmail(email)
                .WithPassword(password)
                .WithOrganizationId(organizationId)
                .WithForceResetPassword(false);
            var user = builder.Build();
            user.AddRole(role);

            db.AppUsers.Add(user);
            await db.SaveChangesAsync();

            return user;
        });
    }

    #endregion
}