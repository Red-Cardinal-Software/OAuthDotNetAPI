using Application.Common.Email;
using Application.Common.Interfaces;
using Application.DTOs.Users;
using Application.Interfaces.Mappers;
using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Application.Interfaces.Security;
using Application.Interfaces.Services;
using Application.Interfaces.Validation;
using Application.Logging;
using Application.Mapper.Base;
using Application.Mapper.Custom;
using Application.Security;
using Application.Services.AppUser;
using Application.Services.Auth;
using Application.Services.Email;
using Application.Services.PasswordReset;
using Application.Validators;
using AutoMapper;
using FluentValidation;
using Infrastructure.Emailing;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Security;
using Infrastructure.Security.Repository;
using Infrastructure.Web.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DependencyInjectionConfiguration;

/// <summary>
/// Class representing configurable options for application-level dependency injection setup.
/// This provides flags to enable or disable specific dependency injection configurations
/// such as repositories, services, database, validation, authorization, and AutoMapper.
/// </summary>
public class AppDependencyOptions
{
    public bool IncludeRepositories { get; set; } = true;
    public bool IncludeServices { get; set; } = true;
    public bool IncludeDb { get; set; } = true;
    public bool IncludeValidation { get; set; } = true;
    public bool IncludeAuthorization { get; set; } = true;
    public bool IncludeAutoMapper { get; set; } = true;
}

/// <summary>
/// Provides extension methods for configuring application-level dependency injection
/// with options to include various components such as repositories, services,
/// database context, validation, authorization policies, and AutoMapper.
/// This class is designed to streamline the registration of dependencies
/// by utilizing a customizable configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures application-level dependency injection by registering various components such as
    /// repositories, services, database context, validation, authorization policies, and AutoMapper.
    /// This method provides customization options to include or exclude specific components.
    /// </summary>
    /// <param name="services">The IServiceCollection instance to which dependencies will be added.</param>
    /// <param name="environment">The IHostEnvironment instance that provides information about the application's hosting environment.</param>
    /// <param name="configure">
    /// An optional Action to configure the <see cref="AppDependencyOptions"/> for determining which components to include.
    /// </param>
    /// <returns>The modified <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddAppDependencies(this IServiceCollection services, IHostEnvironment environment, Action<AppDependencyOptions>? configure = null)
    {
        var options = new AppDependencyOptions();
        configure?.Invoke(options);

        if (options.IncludeDb) services.AddDbContext(environment);
        if (options.IncludeAutoMapper) services.AddAutoMapper();
        services.AddCoreInfrastructure(); // assumed to always be needed
        if (options.IncludeRepositories) services.AddRepositories();
        if (options.IncludeServices) services.AddServices();
        if (options.IncludeAuthorization) services.AddAuthorizationPolicies();
        if (options.IncludeValidation) services.AddValidation();

        return services;
    }

    /// <summary>
    /// Registers repository-related services with the dependency injection container.
    /// This includes implementations for user management, password management,
    /// token management, role management, email templates, and email template rendering.
    /// </summary>
    /// <param name="services">The IServiceCollection instance to which repository services will be added.</param>
    /// <returns>The modified IServiceCollection instance after adding repository services.</returns>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IBlacklistedPasswordRepository, BlacklistedPasswordRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();

        return services;
    }

    /// <summary>
    /// Registers service layer dependencies to the dependency injection container.
    /// This includes application-specific services, mappers, and utilities required for the application functionality.
    /// </summary>
    /// <param name="services">The IServiceCollection instance where dependencies will be registered.</param>
    /// <returns>The modified IServiceCollection instance with the registered services.</returns>
    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAppUserMapper, AppUserMapper>();
        services.AddScoped<IPasswordResetEmailService, PasswordResetEmailService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IAppUserService, AppUserService>();
        services.AddScoped<IEmailService, NotImplementedEmailService>(); // Replace it with your own implementation

        return services;
    }

    /// <summary>
    /// Adds custom authorization policies and their associated handlers to the service collection.
    /// This enables fine-grained authorization by configuring policies and associating them with handlers.
    /// </summary>
    /// <param name="services">
    /// The IServiceCollection instance used for registering the authorization policy provider
    /// and policy handlers.
    /// </param>
    /// <returns>The modified IServiceCollection instance.</returns>
    private static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PrivilegePolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PrivilegeAuthorizationHandler>();

        return services;
    }

    /// <summary>
    /// Adds and configures the database context and related persistence components, such as the unit of work
    /// and CRUD operators, for dependency injection in the service collection.
    /// The database context is configured with environment-specific options, such as enabling sensitive data
    /// logging in development.
    /// </summary>
    /// <param name="services">The IServiceCollection instance to which the database context and persistence components will be added.</param>
    /// <param name="environment">The IHostEnvironment instance that provides information about the application's hosting environment.</param>
    /// <returns>The modified IServiceCollection instance with the registered database and persistence components.</returns>
    private static IServiceCollection AddDbContext(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            // Only allow sensitive data logging when in development
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
            }
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(ICrudOperator<>), typeof(CrudOperator<>));

        return services;
    }

    /// <summary>
    /// Registers AutoMapper into the dependency injection container by configuring
    /// and adding the necessary AutoMapper components, including creating a MapperConfiguration
    /// and setting up a scoped instance of IMapper.
    /// </summary>
    /// <param name="services">The IServiceCollection instance to which AutoMapper will be added.</param>
    /// <returns>The modified IServiceCollection instance with AutoMapper configured.</returns>
    private static IServiceCollection AddAutoMapper(this IServiceCollection services)
    {
        var automapperConfig = new MapperConfiguration(config =>
        {
            config.AddProfile(new BaseProfile());
        }, new LoggerFactory());

        services.AddScoped(provider => automapperConfig.CreateMapper());

        return services;
    }

    /// <summary>
    /// Configures core infrastructure dependencies required by the application.
    /// This includes services such as HttpContextAccessor, logging, and scoped LogContextHelper for improved logging capabilities.
    /// </summary>
    /// <param name="services">The IServiceCollection instance to which core infrastructure dependencies will be registered.</param>
    /// <returns>The modified IServiceCollection instance.</returns>
    private static IServiceCollection AddCoreInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddLogging();
        services.AddScoped(typeof(LogContextHelper<>));

        return services;
    }

    /// <summary>
    /// Registers validation-related services and configurations, enabling dependency injection for validation response factories
    /// and typed validators tailored to specific DTO types.
    /// </summary>
    /// <param name="services">The IServiceCollection instance to which validation services will be added.</param>
    /// <returns>The modified IServiceCollection instance.</returns>
    private static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddScoped<IValidationResponseFactory, ProblemDetailsValidationResponseFactory>();
        services.AddTypedValidation<PasswordValidator, string>();
        services.AddTypedValidation<NewUserValidator, CreateNewUserDto>();
        services.AddTypedValidation<UpdateUserValidator, AppUserDto>();

        return services;
    }

    /// <summary>
    /// Registers a strongly-typed validator for a specific data transfer object (DTO) type in the application's dependency injection container.
    /// This method ensures that the specified <typeparamref name="TAbstractValidator"/> and corresponding <typeparamref name="TDto"/> are properly registered
    /// for use within the application's validation pipeline.
    /// </summary>
    /// <typeparam name="TAbstractValidator">The type of the abstract validator to be registered. Must inherit from <see cref="FluentValidation.AbstractValidator{TDto}"/>.</typeparam>
    /// <typeparam name="TDto">The type of the DTO that the validator validates.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to which the validator will be added.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> instance.</returns>
    private static IServiceCollection AddTypedValidation<TAbstractValidator, TDto>(this IServiceCollection services)
        where TAbstractValidator : AbstractValidator<TDto> => services.AddScoped<TAbstractValidator>()
        .AddScoped<IValidator<TDto>, TAbstractValidator>(x => x.GetService<TAbstractValidator>() ?? throw new InvalidOperationException());
}