using System.Net;
using System.Net.Http.Json;
using Application.DTOs.Auth;
using Domain.Entities.Identity;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestUtils.EntityBuilders;
using TestUtils.Utilities;
using WebApi.Integration.Tests.Fixtures;

namespace WebApi.Integration.Tests.Auth;

/// <summary>
/// Tests for rate limiting behavior. Uses a separate factory with strict rate limits.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class RateLimitingTests(SqlServerContainerFixture dbFixture) : IAsyncLifetime
{
    private RateLimitedWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    // Rate limit is configured to 3 requests per minute for auth endpoints
    private const int AuthRateLimit = 3;

    public async Task InitializeAsync()
    {
        _factory = new RateLimitedWebApplicationFactory(dbFixture);
        _client = _factory.CreateClient();

        // Initialize database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await dbContext.Database.EnsureCreatedAsync();
        });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task AuthEndpoint_ExceedingRateLimit_Returns429()
    {
        // Arrange - Create a test user
        await EnsureTestOrganizationAsync();
        await CreateTestUserAsync();

        var loginRequest = new UserLoginDto
        {
            Username = "ratelimit-test@example.com",
            Password = "TestPassword123!"
        };

        // Act - Make requests up to the limit (should succeed)
        for (var i = 0; i < AuthRateLimit; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
                $"request {i + 1} of {AuthRateLimit} should not be rate limited");
        }

        // Make one more request that exceeds the limit
        var rateLimitedResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        rateLimitedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests,
            "request exceeding rate limit should return 429");
    }

    [Fact]
    public async Task AuthEndpoint_WithinRateLimit_Succeeds()
    {
        // Arrange
        await EnsureTestOrganizationAsync();
        var user = await CreateTestUserAsync("within-limit@example.com");

        var loginRequest = new UserLoginDto
        {
            Username = "within-limit@example.com",
            Password = "TestPassword123!"
        };

        // Act - Make requests within the limit
        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < AuthRateLimit; i++)
        {
            responses.Add(await _client.PostAsJsonAsync("/api/auth/login", loginRequest));
        }

        // Assert - All should succeed (not be rate limited)
        foreach (var response in responses)
        {
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }
    }

    [Fact]
    public async Task RateLimitedResponse_ContainsRetryAfterHeader()
    {
        // Arrange
        await EnsureTestOrganizationAsync();
        await CreateTestUserAsync("retry-header@example.com");

        var loginRequest = new UserLoginDto
        {
            Username = "retry-header@example.com",
            Password = "TestPassword123!"
        };

        // Exhaust the rate limit
        for (var i = 0; i < AuthRateLimit; i++)
        {
            await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        }

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.Contains("Retry-After").Should().BeTrue(
            "rate limited response should include Retry-After header");
    }

    #region Helper Methods

    private async Task EnsureTestOrganizationAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var orgId = TestConstants.Ids.OrganizationId;
        var existing = await db.Set<Organization>().FindAsync(orgId);
        if (existing is not null)
            return;

        var org = new Organization("Test Organization");
        typeof(Organization).GetProperty("Id")!.SetValue(org, orgId);
        db.Set<Organization>().Add(org);
        await db.SaveChangesAsync();
    }

    private async Task<AppUser> CreateTestUserAsync(string email = "ratelimit-test@example.com")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new AppUserBuilder()
            .WithEmail(email)
            .WithPassword("TestPassword123!")
            .WithForceResetPassword(false)
            .Build();

        db.AppUsers.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    #endregion
}