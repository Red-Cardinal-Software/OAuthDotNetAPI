# Starbase

Your new secure .NET API.

## Getting Started

```bash
# Start dependencies
docker-compose -f docker-compose.deps.yml up -d

# Run the API
dotnet run --project WebApi

# Run tests
dotnet test
```

## Configuration

Key settings in `WebApi/appsettings.json`:

| Setting | Description |
|---------|-------------|
| `ConnectionStrings:SqlConnection` | Database connection string |
| `AppSettings:JwtSigningKey` | JWT signing key (min 32 chars) |
| `AppSettings:JwtIssuer` | Token issuer URL |
| `AppSettings:JwtAudience` | Token audience |

## Project Structure

```
Domain/           → Entities, value objects, domain logic
Application/      → Business logic, services, DTOs
Infrastructure/   → Data access, EF Core, repositories
WebApi/           → Controllers, middleware, API config
```

## Documentation

For detailed documentation on security features, MFA setup, audit logging, and more, visit the original template documentation at **[View Full Documentation →](https://red-cardinal-software.github.io/Secure-DotNet-Clean-Architecture/)**.

## License

MIT License