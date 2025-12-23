# Starbase

A secure, enterprise-ready .NET API template with Clean Architecture. Batteries included.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Quick Start

```bash
# Install the template
dotnet new install .

# Create a new project
dotnet new starbase -n MyCompanyApi

# Run it
cd MyCompanyApi
dotnet run --project WebApi
```

## What's Included

### Security (Production-Ready)

- **JWT Authentication** with secure refresh token rotation
- **Multi-Factor Authentication** – TOTP, Email, WebAuthn/Passkeys, Push notifications
- **Rate Limiting** – Per-endpoint policies (auth, API, password reset, MFA)
- **Account Lockout** – Progressive lockout with exponential backoff
- **Security Headers** – CSP, HSTS, X-Frame-Options, and more
- **CORS** – Strict configuration with environment-specific origins

### Enterprise Features

- **Audit Logging** – Hash-chained ledger with SQL Server partitioning
- **Health Checks** – Kubernetes-ready liveness and readiness probes
- **Observability** – Serilog structured logging + OpenTelemetry tracing
- **Docker Support** – Multi-stage build with non-root user

### Architecture

Clean Architecture with four layers:

```
Domain/           → Entities, value objects, domain logic
Application/      → Business logic, services, DTOs
Infrastructure/   → Data access, external services, EF Core
WebApi/           → Controllers, middleware, API configuration
```

## Documentation

**[View Full Documentation →](https://red-cardinal-software.github.io/Secure-DotNet-Clean-Architecture/)**

| Topic | Description |
|-------|-------------|
| [Getting Started](docs/getting-started.md) | Installation and first run |
| [Authentication](docs/authentication/index.md) | JWT, MFA, WebAuthn |
| [Security](docs/security/rate-limiting.md) | Rate limiting, headers |
| [Audit Logging](docs/audit-logging.md) | Enterprise audit trail |
| [Docker](docs/docker.md) | Container deployment |
| [Configuration](docs/configuration.md) | All settings reference |

## Development

```bash
# Start dependencies (SQL Server + Redis)
docker-compose -f Starbase/docker-compose.deps.yml up -d

# Run the API
dotnet run --project Starbase/WebApi

# Run tests
dotnet test Starbase/
```

## Philosophy

**Secure by default.** Security features are enabled out of the box. OpenAPI is development-only. CORS is strict. Headers are configured.

**Opt-out, not opt-in.** Disable features explicitly if you don't need them, rather than remembering to enable them.

**Raise the security floor.** If a 3-person startup can ship with MFA, rate limiting, and proper audit logging, the world has better cybersecurity.

## License

MIT License. See [LICENSE](LICENSE) for details.

---

Built with assistance from Claude AI.