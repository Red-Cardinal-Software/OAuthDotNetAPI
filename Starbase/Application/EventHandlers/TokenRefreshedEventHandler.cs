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
/// Handles TokenRefreshedEvent by recording to the audit ledger.
/// </summary>
public class TokenRefreshedEventHandler : INotificationHandler<TokenRefreshedEvent>
{
    private readonly IAuditLedger _auditLedger;
    private readonly IAuditQueue _auditQueue;
    private readonly AuditOptions _options;
    private readonly ILogger<TokenRefreshedEventHandler> _logger;

    public TokenRefreshedEventHandler(
        IAuditLedger auditLedger,
        IAuditQueue auditQueue,
        IOptions<AuditOptions> options,
        ILogger<TokenRefreshedEventHandler> logger)
    {
        _auditLedger = auditLedger;
        _auditQueue = auditQueue;
        _options = options.Value;
        _logger = logger;
    }

    public async Task Handle(TokenRefreshedEvent notification, CancellationToken cancellationToken)
    {
        var auditEntry = new CreateAuditEntryDto
        {
            EventType = AuditEventType.Authentication,
            Action = AuditAction.TokenRefresh,
            Success = notification.Success,
            FailureReason = notification.FailureReason,
            UserId = notification.UserId,
            Username = notification.Username,
            IpAddress = notification.IpAddress,
            CorrelationId = notification.CorrelationId,
            EntityType = "User",
            EntityId = notification.UserId.ToString()
        };

        if (_options.ProcessingMode == AuditProcessingMode.Batched)
        {
            await _auditQueue.EnqueueAsync(auditEntry, cancellationToken);
        }
        else
        {
            var result = await _auditLedger.RecordAsync(auditEntry);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to record token refresh audit event for {Username}: {Message}",
                    notification.Username, result.Message);
            }
        }
    }
}