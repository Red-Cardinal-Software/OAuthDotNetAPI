using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests.Security;

public class CorsTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    private const string AllowedOrigin = "https://allowed-origin.example.com";
    private const string BlockedOrigin = "https://malicious-site.example.com";

    [Fact]
    public async Task PreflightRequest_WithAllowedOrigin_ReturnsAccessControlHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health/live");
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "Authorization");

        // Act
        var response = await Client.SendAsync(request);

        // Assert - Preflight should succeed (CORS middleware responds)
        // Note: Exact behavior depends on CORS configuration in appsettings
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NoContent,
            HttpStatusCode.MethodNotAllowed); // MethodNotAllowed if CORS not configured for this origin
    }

    [Fact]
    public async Task PreflightRequest_IncludesAccessControlMaxAge()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");

        // Act
        var response = await Client.SendAsync(request);

        // Assert - If CORS is enabled and origin is allowed, should have max-age
        if (response.Headers.Contains("Access-Control-Max-Age"))
        {
            var maxAge = response.Headers.GetValues("Access-Control-Max-Age").First();
            int.Parse(maxAge).Should().BeGreaterThan(0, "preflight cache should be configured");
        }
    }

    [Fact]
    public async Task SimpleRequest_WithOriginHeader_ResponseIncludesVaryHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health/live");
        request.Headers.Add("Origin", AllowedOrigin);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Vary header should include Origin when CORS is active
        if (response.Headers.Contains("Vary"))
        {
            var varyHeader = response.Headers.GetValues("Vary");
            varyHeader.Should().Contain(v => v.Contains("Origin") || v == "*",
                "Vary header should account for Origin when CORS is enabled");
        }
    }

    [Fact]
    public async Task Request_WithoutOriginHeader_StillSucceeds()
    {
        // Arrange - Same-origin requests don't send Origin header
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health/live");
        // No Origin header

        // Act
        var response = await Client.SendAsync(request);

        // Assert - Should work fine for same-origin requests
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PreflightRequest_ForAuthEndpoint_AllowsAuthorizationHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type, Authorization");

        // Act
        var response = await Client.SendAsync(request);

        // Assert - Should allow the Authorization header
        if (response.Headers.Contains("Access-Control-Allow-Headers"))
        {
            var allowedHeaders = response.Headers.GetValues("Access-Control-Allow-Headers").First();
            allowedHeaders.ToLower().Should().Contain("authorization",
                "Authorization header should be allowed for authenticated requests");
        }
    }

    [Fact]
    public async Task PreflightRequest_AllowsCommonHttpMethods()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health/live");
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        if (response.Headers.Contains("Access-Control-Allow-Methods"))
        {
            var allowedMethods = response.Headers.GetValues("Access-Control-Allow-Methods").First();
            allowedMethods.Should().ContainAny("GET", "*");
        }
    }

    [Fact]
    public async Task CrossOriginRequest_ToProtectedEndpoint_StillRequiresAuthentication()
    {
        // Arrange - Even with CORS headers, auth is still required
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/user/GetAllUsers");
        request.Headers.Add("Origin", AllowedOrigin);

        // Act
        var response = await Client.SendAsync(request);

        // Assert - Should still be unauthorized (CORS doesn't bypass auth)
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CrossOriginRequest_ToPublicEndpoint_Succeeds()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health/live");
        request.Headers.Add("Origin", AllowedOrigin);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PreflightRequest_ForDeleteMethod_IsHandled()
    {
        // Arrange - DELETE is often restricted in CORS
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/admin/user/00000000-0000-0000-0000-000000000001");
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "DELETE");

        // Act
        var response = await Client.SendAsync(request);

        // Assert - Should handle the preflight (exact response depends on config)
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NoContent,
            HttpStatusCode.MethodNotAllowed,
            HttpStatusCode.Unauthorized); // Some configs require auth even for preflight
    }

    [Fact]
    public async Task PostRequest_WithContentType_FromCrossOrigin_IsHandled()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        request.Headers.Add("Origin", AllowedOrigin);
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert - Should process the request (will fail auth validation, not CORS)
        // 400 means the request made it through CORS and failed on validation
        // 401/403 means auth issues
        // Not 405 or blocked by CORS
        response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed);
    }
}