using System.Net;
using FluentAssertions;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests.Security;

public class SecurityHeadersTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    [Fact]
    public async Task Response_ContainsXFrameOptionsHeader()
    {
        // Act
        var response = await Client.GetAsync("/api/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.GetValues("X-Frame-Options").First().Should().Be("DENY");
    }

    [Fact]
    public async Task Response_ContainsXContentTypeOptionsHeader()
    {
        // Act
        var response = await Client.GetAsync("/api/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");
    }

    [Fact]
    public async Task Response_ContainsXXssProtectionHeader()
    {
        // Act
        var response = await Client.GetAsync("/api/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-XSS-Protection");
        response.Headers.GetValues("X-XSS-Protection").First().Should().Be("1; mode=block");
    }

    [Fact]
    public async Task Response_ContainsReferrerPolicyHeader()
    {
        // Act
        var response = await Client.GetAsync("/api/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("Referrer-Policy");
        response.Headers.GetValues("Referrer-Policy").First().Should().Be("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task Response_ContainsPermissionsPolicyHeader()
    {
        // Act
        var response = await Client.GetAsync("/api/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("Permissions-Policy");
        var permissionsPolicy = response.Headers.GetValues("Permissions-Policy").First();
        permissionsPolicy.Should().Contain("camera=()");
        permissionsPolicy.Should().Contain("microphone=()");
        permissionsPolicy.Should().Contain("geolocation=()");
    }

    [Fact]
    public async Task Response_ContainsXPermittedCrossDomainPoliciesHeader()
    {
        // Act
        var response = await Client.GetAsync("/api/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-Permitted-Cross-Domain-Policies");
        response.Headers.GetValues("X-Permitted-Cross-Domain-Policies").First().Should().Be("none");
    }

    [Fact]
    public async Task Response_DoesNotContainServerHeader()
    {
        // Act
        var response = await Client.GetAsync("/api/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().NotContainKey("Server");
    }

    [Fact]
    public async Task Response_DoesNotContainXPoweredByHeader()
    {
        // Act
        var response = await Client.GetAsync("/api/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().NotContainKey("X-Powered-By");
    }

    [Fact]
    public async Task Response_DoesNotContainXAspNetVersionHeader()
    {
        // Act
        var response = await Client.GetAsync("/api/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().NotContainKey("X-AspNet-Version");
    }

    [Fact]
    public async Task SecurityHeaders_PresentOnErrorResponses()
    {
        // Act - Request a protected endpoint without auth to get 401
        var response = await Client.GetAsync("/api/admin/user/GetAllUsers");

        // Assert - Security headers should still be present
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.Should().ContainKey("X-XSS-Protection");
    }

    [Fact]
    public async Task SecurityHeaders_PresentOnNotFoundResponses()
    {
        // Act
        var response = await Client.GetAsync("/api/nonexistent/endpoint");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("X-Content-Type-Options");
    }
}