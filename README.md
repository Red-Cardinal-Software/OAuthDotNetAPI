# Starbase
### A Secure .NET Clean Architecture Template

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
- **Multi-Factor Authentication (MFA)** with TOTP, WebAuthn/FIDO2, Email MFA, and recovery codes
- **Account Lockout Protection** with exponential backoff and configurable policies
- **Comprehensive Rate Limiting** with IP-based throttling and configurable policies
- **Production Health Check Endpoints** with privilege-based access controls
- **Validation with FluentValidation**, including async + injected validators
- **Well-commented for learning** and onboarding new developers
- **Built for Azure or Cloud Hosting**
- **Unit of Work & Repository Patterns**
- **Value Objects and Guarded Entities**

## Project Structure

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

## Getting Started

Clone this repository

```bash
git clone https://github.com/Red-Cardinal-Software/Secure-DotNet-Clean-Architecture.git
cd Secure-DotNet-Clean-Architecture
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

## Observability & Logging

This template includes enterprise-grade observability and logging capabilities designed for production environments.

### Structured Logging with Serilog

The application uses Serilog for comprehensive structured logging with rich context and flexible output targets.

**Key Features:**
- **Structured logging** with enriched context for detailed tracing
- **Environment-aware output** - Console for development, configurable sinks for production
- **Configuration-driven setup** via `appsettings.json`
- **Log context enrichment** for request correlation and user tracking
- **Production-ready** with configurable log levels and filtering

**Configuration:**
Logging is configured in `WebApi/Program.cs` with environment-aware sink selection:
```csharp
var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext();

// Only write to console in development - production should use configured sinks
if (builder.Environment.IsDevelopment())
{
    loggerConfig.WriteTo.Console();
}

Log.Logger = loggerConfig.CreateLogger();
```

**Serilog Dependencies:**
- `Serilog.AspNetCore` - ASP.NET Core integration
- `Serilog.Sinks.Console` - Console output sink

**Custom Logging Components:**
- `LogContextHelper<T>` - Structured context building
- `StructuredLogBuilder` - Fluent log entry construction
- Context enrichment for user ID, request ID, and operation tracking

**Development Configuration:**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "Enrich": ["FromLogContext"]
  }
}
```

**Production Configuration Example:**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "/var/log/oauth-api/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      },
      {
        "Name": "ApplicationInsights",
        "Args": {
          "connectionString": "your-application-insights-connection-string"
        }
      }
    ],
    "Enrich": ["FromLogContext"],
    "Properties": {
      "Application": "OAuth.NET.API",
      "Environment": "Production"
    }
  }
}
```

### OpenTelemetry Instrumentation

Production-ready observability with comprehensive distributed tracing and metrics collection.

**Instrumentation Coverage:**
- **ASP.NET Core** - HTTP request/response tracing
- **HTTP Client** - Outbound HTTP call tracing  
- **SQL Client** - Database operation tracing
- **Custom traces** - Application-specific tracing with source "StarbaseTemplateAPI"

**Export Options:**
- **OTLP (OpenTelemetry Protocol)** - Production telemetry export
- **Console Exporter** - Development debugging (development environment only)

**Implementation:**
Located in `DependencyInjectionConfiguration/ServiceCollectionExtensions.cs`:
```csharp
private static IServiceCollection AddOpenTelemetryObservability(this IServiceCollection services, IHostEnvironment environment)
{
    services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddSource("StarbaseTemplateAPI")
                .AddOtlpExporter();

            if (environment.IsDevelopment())
            {
                tracing.AddConsoleExporter();
            }
        })
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation());

    return services;
}
```

**OpenTelemetry Dependencies:**
- `OpenTelemetry.Extensions.Hosting` - Hosting integration
- `OpenTelemetry.Instrumentation.AspNetCore` - ASP.NET Core instrumentation
- `OpenTelemetry.Instrumentation.Http` - HTTP client instrumentation

**Benefits:**
- **End-to-end visibility** - Full request tracing across service boundaries
- **Performance monitoring** - Identify bottlenecks and latency issues
- **Error tracking** - Comprehensive error context and stack traces
- **Metrics collection** - HTTP request metrics, response times, error rates
- **Integration ready** - Compatible with Jaeger, Zipkin, Azure Application Insights, and other OTLP-compatible backends

**Environment Configuration:**
Configure OTLP export endpoints via environment variables:
```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://your-collector:4317
OTEL_SERVICE_NAME=oauth-api
OTEL_RESOURCE_ATTRIBUTES=service.version=1.0.0,deployment.environment=production
```

**Custom Tracing:**
Add custom traces in your services:
```csharp
using var activity = ActivitySource.StartActivity("CustomOperation");
activity?.SetTag("user.id", userId);
activity?.SetTag("operation.type", "mfa-verification");
// Your business logic here
```

### Production Monitoring Setup

**Centralized Logging:**
- Configure Serilog sinks for your preferred logging platform (ELK Stack, Splunk, Azure Application Insights)
- Use structured logging properties for effective querying and alerting

**Distributed Tracing:**
- Deploy OpenTelemetry Collector for production telemetry processing
- Configure sampling rates to manage data volume
- Set up dashboards in your observability platform (Grafana, DataDog, New Relic)

**Log Correlation:**
- Request IDs automatically correlate logs across components
- User context enriches logs for audit trails
- Exception context provides detailed error information

## Audit Logging

This template includes an enterprise-grade audit logging system designed for compliance, security monitoring, and forensic analysis. The audit ledger uses a tamper-evident hash chain and integrates with SQL Server 2022+ Ledger tables for cryptographic verification.

### Architecture

**Dual Audit Strategy:**
| Mechanism | Purpose | Captures |
|-----------|---------|----------|
| **Entity Interceptor** | Tracks data changes | User created/updated, Role changes, MFA enabled |
| **Domain Events** | Tracks business actions | Login attempts, Logout, Token refresh, Password reset |

**Key Features:**
- **Hash Chain Integrity** – Each audit entry includes a SHA-256 hash of the previous entry, creating a tamper-evident chain
- **SQL Server Ledger Tables** – Cryptographic verification using database-level append-only ledger (SQL Server 2022+)
- **Monthly Partitioning** – Automatic partition management for performance and archival
- **Configurable Processing** – Sync (reliable) or Batched (high-performance) modes
- **Domain Event Integration** – MediatR-based events for extensibility (SIEM, notifications)

### Configuration

Audit settings in `appsettings.json`:

```json
{
  "Audit": {
    "ProcessingMode": "Sync",
    "BatchSize": 100,
    "FlushIntervalMs": 5000,
    "EnableConsoleLogging": false
  },
  "AuditArchive": {
    "Enabled": true,
    "CheckInterval": "01:00:00",
    "AddPartitionOnDay": 25,
    "ArchiveOnDay": 5,
    "MonthsToKeepBeforeArchive": 2,
    "AutoPurgeAfterArchive": true,
    "MinWaitBeforePurge": "1.00:00:00",
    "RetentionPolicy": "default"
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| **ProcessingMode** | Sync | `Sync` for reliability, `Batched` for high-throughput |
| **BatchSize** | 100 | Entries per batch (Batched mode only) |
| **FlushIntervalMs** | 5000 | Max time before flush (Batched mode only) |
| **AddPartitionOnDay** | 25 | Day of month to add next partition |
| **MonthsToKeepBeforeArchive** | 2 | Months to retain before archiving |

### Processing Modes

**Sync Mode (Default):**
- Audit entry written immediately with each event
- Transactional consistency – audit entry guaranteed if action succeeds
- Best for: Compliance requirements, smaller deployments

**Batched Mode:**
- Events queued in-memory using `Channel<T>`
- Background service flushes batches to database
- Best for: High-throughput applications, eventual consistency acceptable

```json
{
  "Audit": {
    "ProcessingMode": "Batched",
    "BatchSize": 100,
    "FlushIntervalMs": 5000
  }
}
```

### Domain Events

Authentication events are captured via MediatR domain events:

| Event | Trigger | Data Captured |
|-------|---------|---------------|
| `LoginAttemptedEvent` | Login success/failure | UserId, Username, IP, Success, FailureReason |
| `LogoutEvent` | User logout | UserId, Username |
| `TokenRefreshedEvent` | Token refresh | UserId, Username, IP |
| `PasswordResetRequestedEvent` | Password reset request | UserId, Email, IP |

**Extensibility:**
Add custom handlers to react to auth events (e.g., SIEM integration, Slack alerts):

```csharp
public class SiemNotificationHandler : INotificationHandler<LoginAttemptedEvent>
{
    public async Task Handle(LoginAttemptedEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.Success)
        {
            await _siemClient.SendSecurityEventAsync(new SecurityEvent
            {
                Type = "FailedLogin",
                Username = notification.Username,
                IpAddress = notification.IpAddress,
                Timestamp = notification.OccurredAt
            });
        }
    }
}
```

### Entity Auditing

Entities marked with `[Audited]` are automatically tracked via EF Core interceptor:

```csharp
[Audited]
public class Role : IEquatable<Role>
{
    // Changes to this entity are automatically audited
}
```

**Currently Audited Entities:**
- `Organization` – Org lifecycle and settings
- `Role` – Role changes
- `Privilege` – Permission changes
- `AccountLockout` – Lockout state changes
- `MfaMethod` – MFA configuration changes
- `MfaPushDevice` – Push device registration
- `WebAuthnCredential` – WebAuthn credential lifecycle

### Audit Ledger Schema

The audit ledger captures comprehensive event data:

| Column | Description |
|--------|-------------|
| `SequenceNumber` | Monotonically increasing sequence |
| `EventId` | Unique event identifier |
| `OccurredAt` | Event timestamp (partition key) |
| `Hash` | SHA-256 hash of entry (includes PreviousHash) |
| `PreviousHash` | Hash of previous entry (chain integrity) |
| `UserId` / `Username` | Acting user |
| `IpAddress` / `UserAgent` | Request context |
| `EventType` | Category (Authentication, DataChange, etc.) |
| `Action` | Specific action (LoginSuccess, LoginFailed, Create, Update) |
| `EntityType` / `EntityId` | Affected entity |
| `OldValues` / `NewValues` | JSON of changed values |
| `Success` / `FailureReason` | Outcome details |

### Partition Management

Partitions are managed automatically:

1. **Initial Setup** – Migration creates 12 months back + 24 months forward
2. **Runtime** – Background service adds new partitions on the 25th of each month
3. **Archival** – Old partitions archived to blob storage (configurable)
4. **Purge** – Archived partitions purged after verification

**Manual Partition Operations:**
```csharp
// Add partition boundary
await auditArchiver.AddPartitionBoundaryAsync(new DateTime(2027, 1, 1));

// Archive a partition
var result = await auditArchiver.ArchivePartitionAsync(
    new DateTime(2024, 1, 1),
    archivedBy: "admin",
    retentionPolicy: "7-year");
```

### Verification

Verify audit chain integrity:

```csharp
// Verify hash chain
var verification = await auditLedger.VerifyChainIntegrityAsync(
    fromSequence: 1,
    toSequence: 1000);

if (!verification.IsValid)
{
    _logger.LogCritical("Audit chain tampered at sequence {Seq}",
        verification.FirstInvalidSequence);
}
```

### Compliance Considerations

**SOC 2 / HIPAA:**
- All authentication events are captured
- Data changes include before/after values
- Hash chain provides tamper evidence
- SQL Server Ledger provides database-level verification

**GDPR:**
- Audit entries include user context for access tracking
- Archival system supports retention policies
- Consider data export requirements for audit data

**Best Practices:**
1. Use Sync mode for compliance-critical deployments
2. Regularly verify hash chain integrity
3. Archive old partitions to immutable storage
4. Monitor for audit chain breaks
5. Include audit data in backup strategy

## Docker Support

The template includes Docker support for containerized deployments.

### Quick Start

```bash
# Build and run full stack (API + SQL Server + Redis)
docker-compose up --build

# Run dependencies only (for local development)
docker-compose -f docker-compose.deps.yml up -d
dotnet run --project WebApi
```

### Files

| File | Purpose |
|------|---------|
| `Dockerfile` | Multi-stage build with non-root user (~200MB image) |
| `.dockerignore` | Excludes build artifacts and secrets from context |
| `docker-compose.yml` | Full stack: API + SQL Server + Redis |
| `docker-compose.deps.yml` | Dependencies only for local development |

### Configuration

Environment variables in `docker-compose.yml`:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ConnectionStrings__SqlConnection=Server=sqlserver;Database=Starbase;...
  - ConnectionStrings__Redis=redis:6379
  - AppSettings__JwtSigningKey=YourSuperSecretKeyThatIsAtLeast32CharactersLong!
```

### Building Standalone

```bash
# Build the image
docker build -t starbase-api .

# Run with external dependencies
docker run -p 5000:8080 \
  -e ConnectionStrings__SqlConnection="your-connection-string" \
  -e AppSettings__JwtSigningKey="your-32-char-key" \
  starbase-api
```

### Production Considerations

- Replace default passwords in `docker-compose.yml`
- Use Docker secrets or environment-specific compose files for credentials
- Consider using Azure Container Apps, AWS ECS, or Kubernetes for production
- The image runs as non-root user `starbase` for security

## Technologies Used
- ASP.NET Core 8
- Entity Framework Core 9
- FluentValidation
- OAuth 2.0 with JWT and Refresh Tokens
- Microsoft.AspNetCore.RateLimiting (built-in .NET 8)
- Serilog (Structured Logging)
- OpenTelemetry (Distributed Tracing & Metrics)
- MediatR (Domain Events)
- SQL Server Ledger Tables (Tamper-evident auditing)
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

## Multi-Factor Authentication (MFA)

This template includes a comprehensive MFA system that integrates seamlessly with the existing authentication flow. The MFA implementation follows security best practices and provides a flexible foundation for various authentication methods.

### Supported MFA Methods

**Currently Implemented:**
- ✅ **TOTP (Time-based One-Time Password)** – Compatible with Google Authenticator, Microsoft Authenticator, Authy, and other RFC 6238-compliant apps
- ✅ **Recovery Codes** – Backup codes for account recovery when primary MFA is unavailable
- ✅ **WebAuthn/FIDO2** – Production-ready passkeys and hardware security keys using Fido2.NetLib
- ✅ **Email-based MFA** – One-time codes sent via email as backup method

**Planned Implementations:**
- 🚧 **SMS-based MFA** – Text message codes (backup only, with security warnings)

### MFA Configuration

MFA settings are configurable via `appsettings.json`:

```json
{
  "MfaSettings": {
    "MaxActiveChallenges": 3,
    "MaxChallengesPerWindow": 5,
    "RateLimitWindowMinutes": 5,
    "ChallengeExpiryMinutes": 5,
    "PromptSetup": true
  },
  "AppName": "YourApp"
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| **MaxActiveChallenges** | 3 | Maximum simultaneous active MFA challenges per user |
| **MaxChallengesPerWindow** | 5 | Maximum challenges created within rate limit window |
| **RateLimitWindowMinutes** | 5 | Time window for challenge rate limiting |
| **ChallengeExpiryMinutes** | 5 | How long MFA challenges remain valid |
| **PromptSetup** | true | Whether to prompt users to set up MFA after login |

### TOTP Setup Flow

**1. QR Code Generation:**
```csharp
// Generate secret and QR code for user
var setupDto = await mfaConfigurationService.StartTotpSetupAsync(
    userId, "user@example.com");

// Returns:
// - Base32 secret for manual entry
// - QR code image (Base64-encoded PNG)
// - otpauth:// URI
// - Setup instructions
```

**2. Code Verification:**
```csharp
// User scans QR code and enters verification code
var result = await mfaConfigurationService.VerifyTotpSetupAsync(
    userId, new VerifyMfaSetupDto 
    { 
        Code = "123456",
        Name = "My Phone" 
    });

// Returns recovery codes for backup access
```

**3. Authentication Flow:**
```csharp
// During login, if user has MFA enabled
var requiresMfa = await mfaAuthenticationService.RequiresMfaAsync(userId);

if (requiresMfa)
{
    // Create MFA challenge instead of completing login
    var challenge = await mfaAuthenticationService.CreateChallengeAsync(
        userId, ipAddress, userAgent);
    
    // User enters code from authenticator app
    var verification = await mfaAuthenticationService.VerifyMfaAsync(
        new CompleteMfaDto 
        { 
            ChallengeToken = challenge.ChallengeToken,
            Code = "654321" 
        });
}
```

### Security Features

**Challenge Security:**
- Challenges expire after 5 minutes by default
- Rate limiting prevents brute force attacks
- Failed attempts are tracked and limited
- Automatic invalidation of other challenges on success

**Recovery Codes:**
- Generated during MFA setup
- One-time use only
- Securely hashed in database
- Can be regenerated when needed

**Multiple Methods:**
- Users can have multiple MFA methods (future: TOTP + WebAuthn)
- Default method selection for convenience
- Fallback to other methods if primary fails

**Administrative Controls:**
- System-wide and organization-scoped MFA statistics
- Cleanup of expired challenges and unverified setups
- Privilege-based access to MFA management

### API Endpoints

**User MFA Management:**
- `POST /api/mfa/setup/totp` – Initiate TOTP setup
- `POST /api/mfa/verify-setup` – Complete TOTP verification
- `GET /api/mfa/overview` – Get user's MFA methods
- `POST /api/mfa/regenerate-recovery` – Generate new recovery codes

**Authentication Integration:**
- `POST /api/auth/mfa/challenge` – Create MFA challenge during login
- `POST /api/auth/mfa/verify` – Verify MFA code to complete login

**Administrative Endpoints:**
- `GET /api/admin/mfa/statistics/system` – System-wide MFA adoption metrics
- `GET /api/admin/mfa/statistics/organization/{id}` – Organization MFA metrics
- `DELETE /api/admin/mfa/cleanup/unverified` – Clean up old unverified setups

### Database Schema

**MfaMethod Table:**
- Stores user's configured MFA methods
- Includes TOTP secrets, method names, enabled status
- Tracks usage statistics and verification status

**MfaChallenge Table:**
- Temporary challenges during authentication
- Challenge tokens, expiration, attempt tracking
- Automatic cleanup of expired challenges

**MfaRecoveryCode Table:**
- One-time recovery codes per MFA method
- Securely hashed, usage tracking
- Linked to parent MFA method

### Integration with Existing Security

**Works with Account Lockout:**
- Failed MFA attempts can trigger account lockout
- Lockout policies apply to MFA verification
- Coordinated with existing rate limiting

**Privilege-Based Access:**
- MFA statistics require `SystemAdministration.Metrics` privilege
- Organization admins can view their org's metrics only
- Follows existing authorization patterns

**Audit Trail:**
- All MFA operations are logged with structured logging
- Integration with existing security monitoring
- Failed attempts tracked for analysis

### Common Scenarios

**Enforce MFA for all users:**
```json
{
  "MfaSettings": {
    "PromptSetup": true
  }
}
```

**Stricter MFA security (shorter timeouts, fewer attempts):**
```json
{
  "MfaSettings": {
    "MaxActiveChallenges": 1,
    "MaxChallengesPerWindow": 3,
    "ChallengeExpiryMinutes": 2
  }
}
```

**Development-friendly settings:**
```json
{
  "MfaSettings": {
    "ChallengeExpiryMinutes": 10,
    "PromptSetup": false
  }
}
```

### Testing MFA

**Test TOTP setup:**
```bash
# Start setup
curl -X POST http://localhost:5000/api/mfa/setup/totp \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"accountName": "test@example.com"}'

# Use authenticator app with QR code, then verify
curl -X POST http://localhost:5000/api/mfa/verify-setup \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"code": "123456", "name": "Test Device"}'
```

### WebAuthn/FIDO2 Implementation

The template includes production-ready WebAuthn support using the Fido2.NetLib library:

**Supported Authenticators:**
- **Platform authenticators**: TouchID, FaceID, Windows Hello, Android biometrics
- **Cross-platform authenticators**: YubiKey, Titan Security Key, SoloKey
- **Passkeys**: iCloud Keychain, Google Password Manager, 1Password

**WebAuthn Configuration:**
```json
{
  "WebAuthn": {
    "Origins": ["https://yourdomain.com", "https://localhost:5000"],
    "RelyingPartyName": "Your App Name",
    "RelyingPartyId": "yourdomain.com",
    "TimestampDriftTolerance": 300000
  }
}
```

**Registration Flow:**
```csharp
// Start registration
var registrationOptions = await webAuthnService.StartRegistrationAsync(
    userId, mfaMethodId, userName, displayName);

// Complete registration with attestation from authenticator
var registrationResult = await webAuthnService.CompleteRegistrationAsync(
    userId, mfaMethodId, challenge, attestationResponse, credentialName, ipAddress, userAgent);
```

**Authentication Flow:**
```csharp
// Start authentication
var authenticationOptions = await webAuthnService.StartAuthenticationAsync(userId);

// Complete authentication with assertion
var authenticationResult = await webAuthnService.CompleteAuthenticationAsync(
    credentialId, challenge, assertionResponse);
```

**Security Features:**
- FIDO2-compliant cryptographic verification
- Clone detection via sign count tracking
- Hardware attestation validation
- Credential management and lifecycle

**API Endpoints:**
- `POST /api/mfa/webauthn/register/start` – Begin credential registration
- `POST /api/mfa/webauthn/register/complete` – Complete registration
- `POST /api/mfa/webauthn/authenticate/start` – Begin authentication
- `POST /api/mfa/webauthn/authenticate/complete` – Complete authentication
- `GET /api/mfa/webauthn/credentials` – List user's credentials
- `DELETE /api/mfa/webauthn/credentials/{id}` – Remove credential

### Email-based MFA Implementation

Provides a backup MFA method with configurable security warnings:

**Email MFA Configuration:**
```json
{
  "EmailMfaSettings": {
    "MaxCodesPerWindow": 3,
    "RateLimitWindowMinutes": 15,
    "CodeExpiryMinutes": 5,
    "CleanupAgeHours": 24,
    "AppName": "Your App Name",
    "EnableSecurityWarnings": true
  }
}
```

**Security Features:**
- Cryptographically secure 8-digit codes
- Rate limiting (3 codes per 15-minute window)
- Short expiration times (5 minutes)
- Secure code hashing in database
- Email address masking for privacy
- Automatic cleanup of expired codes

**Flow:**
```csharp
// Send email code
var sendResult = await emailMfaService.SendCodeAsync(
    challengeId, userId, emailAddress, ipAddress);

// Verify code
var verifyResult = await emailMfaService.VerifyCodeAsync(
    challengeId, code);
```

**API Endpoints:**
- `POST /api/mfa/email/send` – Send verification code
- `POST /api/mfa/email/verify` – Verify code
- `GET /api/mfa/email/rate-limit` – Check rate limit status

**Security Warnings:**
Email MFA includes built-in security education:
- Warning about email being less secure than other methods
- Recommendation to use as backup only
- Guidance on email account security

### Future Enhancements

The MFA system is designed for extensibility:

1. **Conditional MFA** – Risk-based authentication (new device, location)
2. **SSO Integration** – External MFA providers (Azure MFA, Duo)

### Security Best Practices

1. **Enable MFA for Privileged Accounts** – Require MFA for admin users
2. **Monitor MFA Bypass Attempts** – Alert on recovery code usage
3. **Regular Cleanup** – Remove old unverified MFA setups
4. **User Education** – Provide clear setup instructions and backup procedures
5. **Recovery Planning** – Ensure admin recovery procedures for locked-out users

## Planned Future Additions
- **Conditional MFA** – Risk-based authentication (new device, location)
- **SSO Integration** – External MFA providers (Azure MFA, Duo)
- **Secure multi-tenant blob storage** – tenant-isolated file storage with access controls

## Contributing
This template is designed to be forked, extended, or modified.  PRs are welcome if you'd like to contribute general purpose improvements

## Licensing
Follows a standard MIT license
