using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests;

public class DatabaseConnectivityTests : IntegrationTestBase
{
    public DatabaseConnectivityTests(SqlServerContainerFixture dbFixture) : base(dbFixture)
    {
    }

    [Fact]
    public async Task Database_Can_Connect()
    {
        // Act & Assert
        await WithDbContextAsync(async dbContext =>
        {
            var canConnect = await dbContext.Database.CanConnectAsync();
            canConnect.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Database_Has_Required_Tables()
    {
        // Act & Assert
        await WithDbContextAsync(async dbContext =>
        {
            // Verify key tables exist by querying them (seeder may have added data)
            var rolesCount = await dbContext.Roles.CountAsync();
            // The table should exist - seeding may have added roles
            rolesCount.Should().BeGreaterThanOrEqualTo(0);

            // Verify other tables exist
            var usersCount = await dbContext.AppUsers.CountAsync();
            usersCount.Should().BeGreaterThanOrEqualTo(0);
        });
    }
}