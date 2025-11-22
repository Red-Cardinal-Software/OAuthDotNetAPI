using System.Globalization;
using System.Threading.RateLimiting;
using Application.Common.Constants;
using Azure.Identity;
using DependencyInjectionConfiguration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddAppDependencies(builder.Environment);
builder.Services.AddControllers();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(builder.Configuration["AppSettings-Token"]!)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    var config = builder.Configuration;

    // Policy for authentication endpoints (login, refresh) - most restrictive
    var authPermitLimit = int.TryParse(config["RateLimiting-Auth-PermitLimit"], out var authLimit)
        ? authLimit
        : SystemDefaults.DefaultRateLimitAuthPermitLimit;

    var authWindowMinutes = int.TryParse(config["RateLimiting-Auth-WindowMinutes"], out var authWindow)
        ? authWindow
        : SystemDefaults.DefaultRateLimitAuthWindowMinutes;

    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = authPermitLimit;
        opt.Window = TimeSpan.FromMinutes(authWindowMinutes);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0; // No queuing - reject immediately when limit exceeded
    });

    // Policy for password reset - moderate restrictions to prevent email spam
    var passwordResetPermitLimit = int.TryParse(config["RateLimiting-PasswordReset-PermitLimit"], out var prLimit)
        ? prLimit
        : SystemDefaults.DefaultRateLimitPasswordResetPermitLimit;

    var passwordResetWindowMinutes = int.TryParse(config["RateLimiting-PasswordReset-WindowMinutes"], out var prWindow)
        ? prWindow
        : SystemDefaults.DefaultRateLimitPasswordResetWindowMinutes;

    options.AddFixedWindowLimiter("password-reset", opt =>
    {
        opt.PermitLimit = passwordResetPermitLimit;
        opt.Window = TimeSpan.FromMinutes(passwordResetWindowMinutes);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    // Policy for general API endpoints - more permissive
    var apiPermitLimit = int.TryParse(config["RateLimiting-Api-PermitLimit"], out var apiLimit)
        ? apiLimit
        : SystemDefaults.DefaultRateLimitApiPermitLimit;

    var apiWindowMinutes = int.TryParse(config["RateLimiting-Api-WindowMinutes"], out var apiWindow)
        ? apiWindow
        : SystemDefaults.DefaultRateLimitApiWindowMinutes;

    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.PermitLimit = apiPermitLimit;
        opt.Window = TimeSpan.FromMinutes(apiWindowMinutes);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    // Global rate limiter - prevents any single IP from overwhelming the API
    var globalPermitLimit = int.TryParse(config["RateLimiting-Global-PermitLimit"], out var globalLimit)
        ? globalLimit
        : SystemDefaults.DefaultRateLimitGlobalPermitLimit;

    var globalWindowMinutes = int.TryParse(config["RateLimiting-Global-WindowMinutes"], out var globalWindow)
        ? globalWindow
        : SystemDefaults.DefaultRateLimitGlobalWindowMinutes;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Partition by IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = globalPermitLimit,
            Window = TimeSpan.FromMinutes(globalWindowMinutes),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    // Customize the response when rate limit is exceeded
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        TimeSpan? retryAfter = null;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
        {
            retryAfter = retry;
            context.HttpContext.Response.Headers.RetryAfter = retry.TotalSeconds.ToString(CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests. Please try again later.",
            retryAfter = retryAfter?.TotalSeconds
        }, cancellationToken);
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Only allow HTTPS
app.UseHttpsRedirection();

// Use only Secure Communication
app.UseHsts();

// Enable rate limiting middleware
app.UseRateLimiter();

app.MapControllers();

app.Run();
