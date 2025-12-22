using Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Background service that runs audit archive operations on a schedule.
/// Archives previous month's partition and adds new partition boundaries.
/// </summary>
public class AuditArchiveBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditArchiveBackgroundService> _logger;
    private readonly AuditArchiveOptions _options;

    public AuditArchiveBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AuditArchiveBackgroundService> logger,
        IOptions<AuditArchiveOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Audit archive background service is disabled");
            return;
        }

        _logger.LogInformation("Audit archive background service started");

        // Wait a bit on startup to let the application fully initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunArchiveWorkflowAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in audit archive workflow");
            }

            // Wait until next check interval
            await Task.Delay(_options.CheckInterval, stoppingToken);
        }

        _logger.LogInformation("Audit archive background service stopped");
    }

    private async Task RunArchiveWorkflowAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var archiver = scope.ServiceProvider.GetRequiredService<IAuditArchiver>();

        var now = DateTime.UtcNow;

        // Add partition for next month if we're past the threshold day
        if (now.Day >= _options.AddPartitionOnDay)
        {
            await EnsureNextPartitionExistsAsync(archiver, now, cancellationToken);
        }

        // Archive previous month if we're past the archive day
        if (now.Day >= _options.ArchiveOnDay)
        {
            await ArchivePreviousMonthAsync(archiver, now, cancellationToken);
        }

        // Purge archived partitions if auto-purge is enabled
        if (_options.AutoPurgeAfterArchive)
        {
            await PurgeArchivedPartitionsAsync(archiver, cancellationToken);
        }
    }

    private async Task EnsureNextPartitionExistsAsync(
        IAuditArchiver archiver,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Add partition for next month
        var nextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);

        try
        {
            await archiver.AddPartitionBoundaryAsync(nextMonth, cancellationToken);
            _logger.LogInformation("Ensured partition exists for {Month}", nextMonth);
        }
        catch (Exception ex) when (ex.Message.Contains("already exists") ||
                                   ex.Message.Contains("duplicate"))
        {
            // Partition already exists, that's fine
            _logger.LogDebug("Partition for {Month} already exists", nextMonth);
        }
    }

    private async Task ArchivePreviousMonthAsync(
        IAuditArchiver archiver,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Archive the month before last (to ensure it's complete)
        // E.g., on March 5th, archive January
        var monthToArchive = new DateTime(now.Year, now.Month, 1)
            .AddMonths(-_options.MonthsToKeepBeforeArchive);

        // Check existing manifests to see if already archived
        var manifests = await archiver.GetArchiveManifestsAsync(
            fromDate: monthToArchive,
            toDate: monthToArchive,
            cancellationToken: cancellationToken);

        if (manifests.Any())
        {
            _logger.LogDebug("Partition {Month} already archived", monthToArchive);
            return;
        }

        _logger.LogInformation("Archiving partition for {Month}", monthToArchive);

        var result = await archiver.ArchivePartitionAsync(
            monthToArchive,
            archivedBy: "AuditArchiveBackgroundService",
            retentionPolicy: _options.RetentionPolicy,
            cancellationToken: cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("Successfully archived partition {Month}, manifest {ManifestId}",
                monthToArchive, result.Manifest?.Id);
        }
        else
        {
            _logger.LogWarning("Failed to archive partition {Month}: {Error}",
                monthToArchive, result.ErrorMessage);
        }
    }

    private async Task PurgeArchivedPartitionsAsync(
        IAuditArchiver archiver,
        CancellationToken cancellationToken)
    {
        // Get manifests that have been archived but not purged
        var manifests = await archiver.GetArchiveManifestsAsync(cancellationToken: cancellationToken);
        var unpurgedManifests = manifests.Where(m => !m.PurgedAt.HasValue).ToList();

        foreach (var manifest in unpurgedManifests)
        {
            // Only purge if archived more than the minimum wait time ago
            if (DateTime.UtcNow - manifest.ArchivedAt < _options.MinWaitBeforePurge)
            {
                _logger.LogDebug("Skipping purge for {Month}, archived too recently",
                    manifest.PartitionBoundary);
                continue;
            }

            _logger.LogInformation("Purging archived partition {Month}", manifest.PartitionBoundary);

            var result = await archiver.PurgePartitionAsync(
                manifest.PartitionBoundary,
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Successfully purged partition {Month}",
                    manifest.PartitionBoundary);
            }
            else
            {
                _logger.LogWarning("Failed to purge partition {Month}: {Error}",
                    manifest.PartitionBoundary, result.ErrorMessage);
            }
        }
    }
}

/// <summary>
/// Configuration options for the audit archive background service.
/// </summary>
public class AuditArchiveOptions
{
    public const string SectionName = "AuditArchive";

    /// <summary>
    /// Whether the background service is enabled. Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How often to check if archiving is needed. Default: 1 hour
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Day of month to add the next partition boundary. Default: 25
    /// </summary>
    public int AddPartitionOnDay { get; set; } = 25;

    /// <summary>
    /// Day of month to run the archive. Default: 5
    /// </summary>
    public int ArchiveOnDay { get; set; } = 5;

    /// <summary>
    /// Number of complete months to keep before archiving. Default: 2
    /// E.g., if 2, on March 5th we archive January (2 months back)
    /// </summary>
    public int MonthsToKeepBeforeArchive { get; set; } = 2;

    /// <summary>
    /// Whether to automatically purge after successful archive. Default: true
    /// </summary>
    public bool AutoPurgeAfterArchive { get; set; } = true;

    /// <summary>
    /// Minimum time to wait after archive before allowing purge. Default: 24 hours
    /// </summary>
    public TimeSpan MinWaitBeforePurge { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Retention policy name to record in manifests. Default: "default"
    /// </summary>
    public string RetentionPolicy { get; set; } = "default";
}