# Secure .NET API Template

A secure, extensible, and production-ready .NET template project built with clean architecture, Domain-Driven Design (DDD), and a strong focus on testability and maintainability.

## Purpose & Context

This template was created to serve as a security-first starting point for building production-grade .NET APIs.  
It incorporates architectural and operational patterns that mitigate common vulnerabilities — including those often seen in on-premises and cloud environments — by enforcing secure defaults, strong authentication flows, and layered protections.

It was informed by years of practical experience in building and securing multi-tenant cloud applications.  
The goal is to make it easier for teams to start secure rather than attempting to retrofit security into an existing codebase.

It can also serve as a reference for security and design practices.

## Core Design Decisions

- **Short-lived access tokens + refresh tokens** — Minimizes risk from token theft.
- **Claims-based and role-based authorization** — Flexible, fine-grained access control.
- **Comprehensive rate limiting** — Protects against brute force attacks and API abuse with configurable IP-based throttling.
- **Guarded domain entities and value objects** — Prevent invalid state and enforce invariants.
- **FluentValidation everywhere** — Input is never trusted by default.
- **Dependency injection-first design** — Encourages testability and clear boundaries.
- **Separation of concerns via Clean Architecture** — Each layer has a single purpose.
- **Security-conscious defaults** — For example:
  - Connection strings are not stored in plain text and can be retrieved from secure stores (e.g., Azure Key Vault).
  - SQL connections use least-privilege accounts by default.
  - Sensitive operations are audited.

## Features

- **Clean Architecture** (Domain, Application, Infrastructure, API)
- **Secure Auth Layer** with refresh tokens and short-lived access tokens
- **Account Lockout Protection** with exponential backoff and configurable policies
- **Comprehensive Rate Limiting** with IP-based throttling and configurable policies
- **Production Health Check Endpoints** with privilege-based access controls
- **Validation with FluentValidation**, including async + injected validators
- **Well-commented for learning** and onboarding new developers
- **Built for Azure or Cloud Hosting**
- **Unit of Work & Repository Patterns**
- **Value Objects and Guarded Entities**

- ## Project Structure

```bash
.
├── tests/                                        # Test Projects for Unit testing
├── Application/                                  # DTOs, Services, Validators, Interfaces
├── DependencyInjectionConfiguration/             # Provides a clean way to add scoped services and other configurations into the Program.cs
├── Domain/                                       # Core domain models and value objects
├── Infrastructure/                               # EF Core, Repositories, Configurations
├── WebApi/                                       # Controllers and API setup
├── README.md                                     # This file
```

Testing
Uses xUnit, FluentAssertions, and Moq

Tests are designed to demonstrate best practices, not just assert behavior

- ## Getting Started

Clone this repository

```bash
git clone [https://github.com/your-username/secure-dotnet-template.git](https://github.com/Red-Cardinal-Software/OAuthDotNetAPI.git)
cd OAuthDotNetAPI
```

Create and configure the database

```bash
dotnet ef database update
```

Run the application

```bash
dotnet run --project WebApi
```

## Rate Limiting

This template includes comprehensive rate limiting to protect against brute force attacks, credential stuffing, and API abuse. Rate limiting is implemented using .NET's built-in `Microsoft.AspNetCore.RateLimiting` middleware.

### Overview

**Two-Layer Protection:**
1. **Policy-Based Limits** – Specific rate limits applied to endpoint groups (auth, password reset, general API)
2. **Global IP-Based Limiter** – Baseline protection across all endpoints to prevent any single IP from overwhelming the server

### Default Rate Limits

| Policy | Endpoints | Limit | Window | Purpose |
|--------|-----------|-------|--------|---------|
| **auth** | Login, Refresh Token | 5 requests | 1 minute | Prevents brute force authentication attacks |
| **password-reset** | Password Reset Request/Submit | 3 requests | 5 minutes | Prevents email spam and abuse |
| **health** | Health Check Endpoints | 30 requests | 1 minute | Prevents health check endpoint abuse |
| **api** | General authenticated endpoints | 100 requests | 1 minute | General API protection |
| **global** | All endpoints (automatic) | 200 requests | 1 minute | Baseline protection per IP address |

### Configuration

All rate limits are configurable via `appsettings.json` without code changes:

```json
{
  "RateLimiting-Auth-PermitLimit": "5",
  "RateLimiting-Auth-WindowMinutes": "1",
  "RateLimiting-PasswordReset-PermitLimit": "3",
  "RateLimiting-PasswordReset-WindowMinutes": "5",
  "RateLimiting-Api-PermitLimit": "100",
  "RateLimiting-Api-WindowMinutes": "1",
  "RateLimiting-Global-PermitLimit": "200",
  "RateLimiting-Global-WindowMinutes": "1"
}
```

**Environment-Specific Configuration:**
- `appsettings.json` – Production defaults (strict)
- `appsettings.Development.json` – Development overrides (more permissive for testing)
- Azure Key Vault – Override for sensitive environments

### Response Format

When rate limit is exceeded, clients receive:

**HTTP 429 Too Many Requests**
```json
{
  "error": "Too many requests. Please try again later.",
  "retryAfter": 60.0
}
```

**Headers:**
- `Retry-After: 60` – Seconds until the client can retry

### Common Scenarios

**More restrictive authentication (3 attempts per minute):**
```json
"RateLimiting-Auth-PermitLimit": "3"
```

**High-traffic production API (500 requests per minute):**
```json
"RateLimiting-Api-PermitLimit": "500",
"RateLimiting-Global-PermitLimit": "1000"
```

**Stricter password reset (1 attempt per 10 minutes):**
```json
"RateLimiting-PasswordReset-PermitLimit": "1",
"RateLimiting-PasswordReset-WindowMinutes": "10"
```

### Adding Custom Rate Limiting Policies

To add a new rate limiting policy:

1. **Define the policy in `Program.cs`:**
```csharp
options.AddFixedWindowLimiter("upload", opt =>
{
    opt.PermitLimit = 10;
    opt.Window = TimeSpan.FromMinutes(5);
    opt.QueueLimit = 0;
});
```

2. **Apply to endpoints:**
```csharp
[HttpPost("upload")]
[EnableRateLimiting("upload")]
public async Task<IActionResult> UploadFile(IFormFile file) => ...
```

### Exempting Endpoints from Rate Limiting

Health check or monitoring endpoints can be exempted:

```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
   .DisableRateLimiting()
   .AllowAnonymous();
```

### Behind a Reverse Proxy?

If your API is behind a reverse proxy (nginx, Azure App Gateway, Cloudflare), configure forwarded headers to ensure rate limiting sees the real client IP:

```csharp
// In Program.cs, before builder.Build()
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// In middleware pipeline, before app.UseRateLimiter()
app.UseForwardedHeaders();
```

### Security Considerations

- **Rate limits are per IP address** – A distributed attack from multiple IPs can still bypass individual limits (consider adding WAF/DDoS protection at the infrastructure layer)
- **BCrypt provides additional protection** – The password hashing work factor adds natural rate limiting to authentication even if rate limits are bypassed
- **Global limiter prevents resource exhaustion** – Even if policy-specific limits are generous, the global limiter prevents any single IP from overwhelming the server

### Testing Rate Limits

Test authentication rate limiting:
```bash
# Make 6 login requests (limit is 5)
for i in {1..6}; do
  curl -X POST http://localhost:5000/api/Auth/login \
    -H "Content-Type: application/json" \
    -d '{"username":"test","password":"test"}' \
    -w "\nStatus: %{http_code}\n"
done
```

Expected: First 5 requests process normally, 6th returns HTTP 429.

## Health Check Endpoints

This template includes production-ready health check endpoints for monitoring application health, readiness, and liveness. The health checks are designed with security in mind, providing different levels of information based on access privileges.

### Available Endpoints

| Endpoint | Access Level | Purpose | Rate Limited |
|----------|-------------|---------|--------------|
| `/api/health` | Public | Basic health status | ✅ (30/min) |
| `/api/health/detailed` | Public | Safe detailed status without sensitive data | ✅ (30/min) |
| `/api/health/live` | Public | Liveness probe for orchestrators | ✅ (30/min) |
| `/api/health/ready` | Public | Readiness probe with database connectivity | ✅ (30/min) |
| `/api/health/system` | Privileged | Complete system metrics (requires `SystemAdministration.Metrics` privilege) | ✅ (30/min) |

### Health Check Configuration

Health checks are configurable via `appsettings.json`:

```json
{
  "HealthChecks": {
    "MemoryThresholdMB": 1024,
    "IncludeMemoryCheck": false
  },
  "RateLimiting-Health-PermitLimit": "30",
  "RateLimiting-Health-WindowMinutes": "1"
}
```

**Security Configuration:**
- `IncludeMemoryCheck: false` – Memory monitoring disabled by default for security
- Memory checks are tagged as "privileged" and only available via `/api/health/system`
- Public endpoints exclude sensitive system information

### Response Examples

**Basic Health Check (`/api/health`):**
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

**Detailed Health Check (`/api/health/detailed`):**
```json
{
  "status": "Healthy",
  "totalDuration": 45.2,
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "duration": 42.1,
      "description": "Service operational"
    }
  ],
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

**System Health Check (`/api/health/system` - Privileged):**
```json
{
  "status": "Healthy",
  "totalDuration": 67.8,
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "duration": 42.1,
      "description": "Service operational",
      "data": {}
    },
    {
      "name": "memory",
      "status": "Healthy",
      "duration": 1.2,
      "description": "Service operational",
      "data": {
        "memoryUsageMB": 512,
        "thresholdMB": 1024
      }
    }
  ],
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

### Health Check Status Codes

- **200 OK** – All health checks passed
- **503 Service Unavailable** – One or more health checks failed
- **429 Too Many Requests** – Rate limit exceeded

### Kubernetes Integration

The health check endpoints are designed for container orchestration:

**Kubernetes Deployment Example:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: oauth-api
spec:
  template:
    spec:
      containers:
      - name: oauth-api
        image: your-api:latest
        ports:
        - containerPort: 8080
        livenessProbe:
          httpGet:
            path: /api/health/live
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /api/health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
```

### Adding Custom Health Checks

To add a new health check:

1. **Create the health check class:**
```csharp
public class CustomHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Your health check logic
        return Task.FromResult(HealthCheckResult.Healthy("Custom service operational"));
    }
}
```

2. **Register in `ServiceCollectionExtensions.cs`:**
```csharp
private static IServiceCollection AddAppHealthChecks(this IServiceCollection services)
{
    services.AddScoped<CustomHealthCheck>();
    
    services.AddHealthChecks()
        .AddCheck<CustomHealthCheck>("custom", 
            failureStatus: HealthStatus.Unhealthy,
            tags: ["external", "ready"]); // Add appropriate tags
    
    return services;
}
```

### Built-in Health Checks

**Database Health Check:**
- Tests database connectivity using `IDbContextFactory`
- 5-second timeout protection
- Performance monitoring (warns if >1000ms)
- Never exposes connection strings or sensitive data
- Tagged for readiness probes

**Memory Health Check (Optional):**
- Monitors managed memory usage via `GC.GetTotalMemory()`
- Configurable threshold (default: 1024MB)
- Tagged as "privileged" for security
- Only available via `/api/health/system` endpoint

### Monitoring and Alerting

**Production Monitoring:**
```bash
# Check basic health
curl -f http://your-api.com/api/health

# Check readiness for load balancer
curl -f http://your-api.com/api/health/ready

# Check detailed status (authenticated)
curl -f -H "Authorization: Bearer $TOKEN" http://your-api.com/api/health/system
```

**Azure Application Insights Integration:**
The health check responses include timing information that integrates well with Application Insights for monitoring and alerting.

## Account Lockout Protection

This template includes a comprehensive account lockout system to protect against brute force attacks, credential stuffing, and unauthorized access attempts. The system implements intelligent lockout policies with exponential backoff and automatic unlocking.

### Overview

**Multi-Layer Defense:**
1. **Automatic Lockout** – Locks accounts after repeated failed login attempts
2. **Exponential Backoff** – Increases lockout duration with continued failed attempts
3. **Manual Lockout** – Administrators can manually lock accounts for security reasons
4. **Login Attempt Tracking** – Detailed audit trail of all login attempts for security analysis

### Default Configuration

Account lockout settings are configurable via `appsettings.json`:

```json
{
  "AccountLockout": {
    "FailedAttemptThreshold": 5,
    "BaseLockoutDurationMinutes": 5,
    "MaxLockoutDurationMinutes": 60,
    "AttemptResetWindowMinutes": 15,
    "EnableAccountLockout": true,
    "TrackLoginAttempts": true
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| **FailedAttemptThreshold** | 5 | Number of failed attempts before lockout |
| **BaseLockoutDurationMinutes** | 5 | Initial lockout duration |
| **MaxLockoutDurationMinutes** | 60 | Maximum lockout duration (caps exponential growth) |
| **AttemptResetWindowMinutes** | 15 | Time window after which failed attempts reset |
| **EnableAccountLockout** | true | Master switch to enable/disable lockout feature |
| **TrackLoginAttempts** | true | Whether to track successful login attempts |

### Lockout Behavior

**Exponential Backoff Strategy:**
- 1st lockout: 5 minutes (base duration)
- 2nd lockout: 10 minutes
- 3rd lockout: 20 minutes
- 4th lockout: 40 minutes
- 5th+ lockout: 60 minutes (max duration)

**Automatic Reset:**
- Failed attempt counter resets after 15 minutes of no attempts
- Accounts automatically unlock when lockout period expires
- Successful login immediately unlocks account and resets counter

### Response Examples

**Failed Login (Not Locked):**
```json
{
  "Success": false,
  "Message": "Invalid username or password",
  "Errors": ["Invalid username or password"],
  "StatusCode": 401,
  "Data": null
}
```

**Failed Login (Account Locked):**
```json
{
  "Success": false,
  "Message": "Account is locked. Please try again later or contact support.",
  "Errors": ["Account is locked. Please try again later or contact support."],
  "StatusCode": 401,
  "Data": null
}
```

### Security Features

**Smart Failure Tracking:**
- Only counts "countable" failures (wrong password)
- Ignores non-security failures (e.g., system errors)
- Tracks IP address and user agent for analysis
- Maintains detailed failure reasons for audit

**Administrative Controls:**
- Manual lock/unlock capabilities
- Custom lockout durations
- Lockout reason documentation
- Tracking of who performed manual locks

**Attack Mitigation:**
- Works in conjunction with rate limiting
- BCrypt password hashing adds computational cost
- Prevents timing attacks by consistent response
- No indication if username exists or not

### Database Schema

The system uses two main entities:

**AccountLockout Table:**
- Tracks lockout state per user
- Records failed attempt counts
- Stores lockout expiration times
- Maintains manual lockout metadata

**LoginAttempt Table:**
- Logs all login attempts (success/failure)
- Records IP address and user agent
- Stores failure reasons
- Enables security analysis and reporting

### Integration Points

**AuthService Integration:**
```csharp
// Automatically called on login failure
await accountLockoutService.RecordFailedAttemptAsync(
    userId, username, ipAddress, userAgent, failureReason);

// Automatically called on successful login
await accountLockoutService.RecordSuccessfulLoginAsync(
    userId, username, ipAddress, userAgent);

// Check lockout status
var isLocked = await accountLockoutService.IsAccountLockedOutAsync(userId);
```

### Common Scenarios

**More aggressive lockout (3 attempts, 30-minute max):**
```json
{
  "AccountLockout": {
    "FailedAttemptThreshold": 3,
    "MaxLockoutDurationMinutes": 30
  }
}
```

**Lenient policy for internal apps (10 attempts, shorter durations):**
```json
{
  "AccountLockout": {
    "FailedAttemptThreshold": 10,
    "BaseLockoutDurationMinutes": 1,
    "MaxLockoutDurationMinutes": 10
  }
}
```

**Disable lockout for development:**
```json
{
  "AccountLockout": {
    "EnableAccountLockout": false
  }
}
```

### Maintenance Operations

**Periodic Cleanup (Recommended: Daily):**
```csharp
// Clean up login attempts older than 90 days
var deletedCount = await accountLockoutService.CleanupOldLoginAttemptsAsync(
    TimeSpan.FromDays(90));

// Process expired lockouts (usually automatic, but can be triggered)
var unlockedCount = await accountLockoutService.ProcessExpiredLockoutsAsync();
```

### Testing Account Lockout

Test lockout behavior:
```bash
# Make 6 failed login attempts (threshold is 5)
for i in {1..6}; do
  curl -X POST http://localhost:5000/api/Auth/login \
    -H "Content-Type: application/json" \
    -d '{"username":"testuser","password":"wrongpassword"}' \
    -w "\nAttempt $i - Status: %{http_code}\n"
  sleep 1
done
```

Expected: First 5 attempts return 401, 6th attempt returns 401 with lockout message.

### Security Best Practices

1. **Monitor Failed Attempts** – Set up alerts for accounts with repeated failures
2. **Review Manual Lockouts** – Audit administrator-initiated lockouts regularly
3. **Analyze Patterns** – Use login attempt data to identify attack patterns
4. **Coordinate with Rate Limiting** – Ensure rate limits complement lockout policies
5. **Consider IP-Based Rules** – May want to add IP-based blocking for persistent attackers
6. **Regular Maintenance** – Clean up old data to maintain performance

## Technologies Used
- ASP.NET Core 8
- Entity Framework Core 9
- FluentValidation
- OAuth 2.0 with JWT and Refresh Tokens
- Microsoft.AspNetCore.RateLimiting (built-in .NET 8)
- xUnit + Moq + FluentAssertions
- Clean Architecture / Domain Driven Design

## Extending the Template
- Add new features by extending the Application.Services layer
- Add new validators using FluentValidation
- Add new entities in the Domain Layer
- Add integration with your cloud provider (Azure, AWS, etc.)

## Special Notes
- Email templating has not been implemented as everyone's needs will likely differ.  The repository only has one example with password resets since it's part of the Auth Flow.  The email service is also not implemented since there are many providers.
- This template uses SQLServer as the database provider, but any other database should work as well.
- Feel free to rename anything that better fits a project you're using it for.
- There are unused methods but were left in as they are likely to be used in general practice at some point
- This was built with experience of many previous Cloud Based Projects

## Planned Future Additions
- **Optional MFA** – TOTP and WebAuthn/passkeys with configurable policy
- **Secure multi-tenant blob storage** – tenant-isolated file storage with access controls

## Contributing
This template is designed to be forked, extended, or modified.  PRs are welcome if you'd like to contribute general purpose improvements

## Licensing
Follows a standard MIT license
