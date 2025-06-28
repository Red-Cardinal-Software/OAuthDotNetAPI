# Secure .NET API Template

A secure, extensible, and production-ready .NET template project built with clean architecture, Domain-Driven Design (DDD), and a strong focus on testability and maintainability.

## Features

- **Clean Architecture** (Domain, Application, Infrastructure, API)
- **Secure Auth Layer** with refresh tokens and short-lived access tokens
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

## Technologies Used
- ASP.NET Core 8
- Entity Framework Core 9
- FluentValidation
- OAuth 2.0 with JWT and Refresh Tokens
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

## Contributing
This template is designed to be forked, extended, or modified.  PRs are welcome if you'd like to contribute general purpose improvements

## Licensing
Follows a standard MIT license
