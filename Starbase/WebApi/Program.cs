using Azure.Identity;
using DependencyInjectionConfiguration;
using Infrastructure.Web.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// SECURITY: Disable the 'Server' header to prevent fingerprinting
// CMMC SI.L2-3.14.1 - Hide system information
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// SECURITY: Only add Swagger services in development to prevent API surface information disclosure
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Starbase Template .NET API",
            Version = "v1",
            Description = "A secure .NET API with comprehensive MFA and security features"
        });

        // Add JWT Bearer Authentication
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter your JWT token in the text input below (without 'Bearer' prefix).",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });
}

/**
// Uncomment this section if using Azure Key Vault
if (builder.Environment.IsProduction())
{
    var keyVaultName = builder.Configuration["KeyVaultName"];
    if (!string.IsNullOrEmpty(keyVaultName))
    {
        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
        builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
    }
}
*/

var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext();

// Only write to console in development - production should use configured sinks
if (builder.Environment.IsDevelopment())
{
    loggerConfig.WriteTo.Console();
}

Log.Logger = loggerConfig.CreateLogger();

builder.Services.AddAppDependencies(builder.Environment, builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Security middleware, order matters!
// 1. HSTS - only in production (tells browsers to always use HTTPS)
if (!app.Environment.IsDevelopment())
{
    // CMMC SI.L2-3.14.1 - System Flaw Identification
    // Use a generic error handler to prevent stack trace leakage
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseSerilogRequestLogging();

// 2. Redirect HTTP to HTTPS
app.UseHttpsRedirection();

// 3. Add security headers (X-Frame-Options, CSP, etc.)
app.UseSecurityHeaders();

// 4. Rate limiting
app.UseRateLimiting();

// 5. CORS - must be before authentication to handle preflight requests
app.UseCors();

// Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
