using Application.Common.Configuration;
using Application.DTOs.Audit;
using Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Background service that processes queued audit entries in batches.
/// Only active when AuditOptions.ProcessingMode is set to Batched.
/// </summary>
public class AuditQueueProcessor : BackgroundService
{
    private readonly IAuditQueue _auditQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditQueueProcessor> _logger;
    private readonly AuditOptions _options;

    public AuditQueueProcessor(
        IAuditQueue auditQueue,
        IServiceProvider serviceProvider,
        IOptions<AuditOptions> options,
        ILogger<AuditQueueProcessor> logger)
    {
        _auditQueue = auditQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Only run if batched mode is enabled
        if (_options.ProcessingMode != AuditProcessingMode.Batched)
        {
            _logger.LogInformation("Audit queue processor disabled - ProcessingMode is {Mode}", _options.ProcessingMode);
            return;
        }

        _logger.LogInformation(
            "Audit queue processor started with BatchSize={BatchSize}, FlushInterval={FlushInterval}ms",
            _options.BatchSize,
            _options.FlushIntervalMs);

        var batch = new List<CreateAuditEntryDto>(_options.BatchSize);
        var lastFlush = DateTime.UtcNow;

        await foreach (var entry in _auditQueue.DequeueAllAsync(stoppingToken))
        {
            batch.Add(entry);

            var timeSinceFlush = (DateTime.UtcNow - lastFlush).TotalMilliseconds;
            var shouldFlush = batch.Count >= _options.BatchSize ||
                              timeSinceFlush >= _options.FlushIntervalMs;

            if (shouldFlush)
            {
                await FlushBatchAsync(batch, stoppingToken);
                batch.Clear();
                lastFlush = DateTime.UtcNow;
            }
        }

        // Flush remaining entries on shutdown
        if (batch.Count > 0)
        {
            await FlushBatchAsync(batch, CancellationToken.None);
        }
    }

    private async Task FlushBatchAsync(List<CreateAuditEntryDto> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var auditLedger = scope.ServiceProvider.GetRequiredService<IAuditLedger>();

            var result = await auditLedger.RecordBatchAsync(batch);

            if (result.Success)
            {
                _logger.LogDebug("Flushed {Count} audit entries to ledger", batch.Count);
            }
            else
            {
                _logger.LogError("Failed to flush {Count} audit entries: {Message}", batch.Count, result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing {Count} audit entries", batch.Count);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Audit queue processor stopping, queue size: {Count}", _auditQueue.Count);
        await base.StopAsync(cancellationToken);
    }
}