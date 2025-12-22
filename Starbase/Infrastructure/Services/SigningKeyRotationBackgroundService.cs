using Application.Common.Configuration;
using Application.Interfaces.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Background service that monitors signing key age and performs automatic rotation when due.
/// Only runs when key rotation is enabled in configuration.
/// </summary>
public class SigningKeyRotationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SigningKeyRotationBackgroundService> _logger;
    private readonly SigningKeyRotationOptions _options;

    public SigningKeyRotationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<SigningKeyRotationOptions> options,
        ILogger<SigningKeyRotationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Signing key rotation is disabled. " +
                "Set SigningKeyRotation:Enabled to true in configuration to enable automatic rotation.");
            return;
        }

        _logger.LogInformation(
            "Signing key rotation background service started. " +
            "Rotation interval: {RotationDays} days, check interval: {CheckMinutes} minutes",
            _options.RotationIntervalDays, _options.CheckIntervalMinutes);

        var checkInterval = TimeSpan.FromMinutes(_options.CheckIntervalMinutes);

        // Initial delay to let the application fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndRotateAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during signing key rotation check. Will retry in {Minutes} minutes.",
                    _options.CheckIntervalMinutes);
            }

            await Task.Delay(checkInterval, stoppingToken);
        }

        _logger.LogInformation("Signing key rotation background service stopped.");
    }

    private async Task CheckAndRotateAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var keyProvider = scope.ServiceProvider.GetService<ISigningKeyProvider>();

        if (keyProvider == null)
        {
            _logger.LogWarning(
                "No ISigningKeyProvider registered. Signing key rotation requires a cloud provider " +
                "(Azure Key Vault, AWS Secrets Manager, or GCP Secret Manager).");
            return;
        }

        var isRotationDue = await keyProvider.IsRotationDueAsync(cancellationToken);

        if (!isRotationDue)
        {
            _logger.LogDebug("Signing key rotation not due. Next check in {Minutes} minutes.",
                _options.CheckIntervalMinutes);
            return;
        }

        _logger.LogInformation("Signing key rotation is due. Starting rotation...");

        try
        {
            var newKeyInfo = await keyProvider.RotateKeyAsync(cancellationToken);

            _logger.LogInformation(
                "Signing key rotation completed successfully. " +
                "New key ID: {KeyId}, valid until: {ExpiresAt}",
                newKeyInfo.KeyId[..Math.Min(16, newKeyInfo.KeyId.Length)] + "...",
                newKeyInfo.ExpiresAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "never");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to rotate signing key. Tokens will continue to be signed with the current key. " +
                "Manual intervention may be required.");
            throw;
        }
    }
}