using System.Net;
using FluentAssertions;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests;

public class HealthEndpointTests : IntegrationTestBase
{
    public HealthEndpointTests(SqlServerContainerFixture dbFixture) : base(dbFixture)
    {
    }

    [Fact]
    public async Task Liveness_Endpoint_Returns_Alive()
    {
        // Act
        var response = await Client.GetAsync("/api/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Alive");
    }

    [Fact]
    public async Task Health_Endpoint_Returns_Success()
    {
        // Act
        var response = await Client.GetAsync("/api/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}