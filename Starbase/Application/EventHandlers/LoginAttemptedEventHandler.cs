using Application.Common.Configuration;
using Application.DTOs.Audit;
using Application.Events.Auth;
using Application.Interfaces.Services;
using Domain.Entities.Audit;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.EventHandlers;

/// <summary>
/// Handles LoginAttemptedEvent by recording to the audit ledger.
/// Supports both sync and batched processing modes.
/// </summary>
public class LoginAttemptedEventHandler : INotificationHandler<LoginAttemptedEvent>
{
    private readonly IAuditLedger _auditLedger;
    private readonly IAuditQueue _auditQueue;
    private readonly AuditOptions _options;
    private readonly ILogger<LoginAttemptedEventHandler> _logger;

    public LoginAttemptedEventHandler(
        IAuditLedger auditLedger,
        IAuditQueue auditQueue,
        IOptions<AuditOptions> options,
        ILogger<LoginAttemptedEventHandler> logger)
    {
        _auditLedger = auditLedger;
        _auditQueue = auditQueue;
        _options = options.Value;
        _logger = logger;
    }

    public async Task Handle(LoginAttemptedEvent notification, CancellationToken cancellationToken)
    {
        var auditEntry = new CreateAuditEntryDto
        {
            EventType = AuditEventType.Authentication,
            Action = notification.Success ? AuditAction.LoginSuccess : AuditAction.LoginFailed,
            Success = notification.Success,
            FailureReason = notification.FailureReason,
            UserId = notification.UserId == Guid.Empty ? null : notification.UserId,
            Username = notification.Username,
            IpAddress = notification.IpAddress,
            UserAgent = notification.UserAgent,
            CorrelationId = notification.CorrelationId,
            EntityType = "User",
            EntityId = notification.UserId == Guid.Empty ? null : notification.UserId.ToString(),
            AdditionalData = BuildAdditionalData(notification)
        };

        if (_options.ProcessingMode == AuditProcessingMode.Batched)
        {
            await _auditQueue.EnqueueAsync(auditEntry, cancellationToken);

            if (_options.EnableConsoleLogging)
            {
                _logger.LogDebug("Queued login audit event for {Username}, Success={Success}",
                    notification.Username, notification.Success);
            }
        }
        else
        {
            var result = await _auditLedger.RecordAsync(auditEntry);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to record login audit event for {Username}: {Message}",
                    notification.Username, result.Message);
            }
            else if (_options.EnableConsoleLogging)
            {
                _logger.LogDebug("Recorded login audit event for {Username}, Success={Success}",
                    notification.Username, notification.Success);
            }
        }
    }

    private static string? BuildAdditionalData(LoginAttemptedEvent notification)
    {
        if (!notification.MfaRequired && !notification.AccountLocked)
            return null;

        var data = new Dictionary<string, object>();

        if (notification.MfaRequired)
            data["mfaRequired"] = true;

        if (notification.AccountLocked)
            data["accountLocked"] = true;

        return System.Text.Json.JsonSerializer.Serialize(data);
    }
}