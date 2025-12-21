using System.Reflection;
using System.Text.Json;
using Application.DTOs.Audit;
using Application.Interfaces.Services;
using Domain.Attributes;
using Domain.Entities.Audit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that automatically tracks entity changes and records them to the audit ledger.
/// Only entities marked with [Audited] attribute will be tracked.
/// </summary>
public class AuditInterceptor(
    IServiceProvider serviceProvider,
    ILogger<AuditInterceptor> logger) : SaveChangesInterceptor
{
    private readonly List<CreateAuditEntryDto> _pendingAuditEntries = [];

    /// <summary>
    /// Entity types to always exclude from automatic auditing regardless of attributes.
    /// </summary>
    private static readonly HashSet<Type> AlwaysExcludedTypes =
    [
        typeof(AuditLedgerEntry) // Don't audit the audit log itself
    ];

    /// <summary>
    /// Default sensitive property names (in addition to [SensitiveData] attribute).
    /// </summary>
    private static readonly HashSet<string> DefaultSensitiveProperties =
    [
        "Password",
        "PasswordHash",
        "SecretKey",
        "TotpSecret",
        "RecoveryCode",
        "Token",
        "RefreshToken",
        "PushToken",
        "PublicKey",
        "PrivateKey"
    ];

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CaptureChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CaptureChanges(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ProcessPendingAuditEntries();
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await ProcessPendingAuditEntriesAsync();
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void CaptureChanges(DbContext? context)
    {
        if (context == null) return;

        var httpContext = GetHttpContext();
        var correlationId = httpContext?.TraceIdentifier;
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();
        var userId = GetUserId(httpContext);
        var username = GetUsername(httpContext);

        foreach (var entry in context.ChangeTracker.Entries())
        {
            var entityType = entry.Entity.GetType();

            // Skip always-excluded types
            if (AlwaysExcludedTypes.Contains(entityType))
                continue;

            // Only audit entities marked with [Audited] attribute
            var auditedAttr = entityType.GetCustomAttribute<AuditedAttribute>();
            if (auditedAttr == null)
                continue;

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var auditEntry = CreateAuditEntry(
                entry,
                auditedAttr,
                userId,
                username,
                ipAddress,
                userAgent,
                correlationId);

            _pendingAuditEntries.Add(auditEntry);
        }
    }

    private CreateAuditEntryDto CreateAuditEntry(
        EntityEntry entry,
        AuditedAttribute auditedAttr,
        Guid? userId,
        string? username,
        string? ipAddress,
        string? userAgent,
        string? correlationId)
    {
        var clrType = entry.Entity.GetType();
        var entityTypeName = auditedAttr.EntityTypeName ?? clrType.Name;
        var entityId = GetEntityId(entry);

        var (action, eventType) = entry.State switch
        {
            EntityState.Added => (AuditAction.DataCreated, AuditEventType.DataChange),
            EntityState.Modified => (AuditAction.DataUpdated, AuditEventType.DataChange),
            EntityState.Deleted => (AuditAction.DataDeleted, AuditEventType.DataChange),
            _ => (AuditAction.DataRead, AuditEventType.DataAccess)
        };

        string? oldValues = null;
        string? newValues = null;

        if (auditedAttr.IncludeOldValues && entry.State is EntityState.Modified or EntityState.Deleted)
        {
            oldValues = SerializeValues(entry, clrType, e => e.OriginalValue);
        }

        if (auditedAttr.IncludeNewValues && entry.State is EntityState.Added or EntityState.Modified)
        {
            newValues = SerializeValues(entry, clrType, e => e.CurrentValue);
        }

        return new CreateAuditEntryDto
        {
            EventType = eventType,
            Action = action,
            Success = true,
            UserId = userId,
            Username = username,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CorrelationId = correlationId,
            EntityType = entityTypeName,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues
        };
    }

    private static string? GetEntityId(EntityEntry entry)
    {
        var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties;
        if (keyProperties == null || keyProperties.Count == 0)
            return null;

        if (keyProperties.Count == 1)
        {
            var value = entry.Property(keyProperties[0].Name).CurrentValue;
            return value?.ToString();
        }

        // Composite key
        var keyValues = keyProperties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
            .ToArray();
        return string.Join(":", keyValues);
    }

    private static string SerializeValues(EntityEntry entry, Type clrType, Func<PropertyEntry, object?> getValue)
    {
        var properties = new Dictionary<string, string?>();

        foreach (var prop in entry.Properties)
        {
            // Skip primary keys
            if (prop.Metadata.IsPrimaryKey())
                continue;

            var propName = prop.Metadata.Name;
            var propInfo = clrType.GetProperty(propName);

            // Skip properties marked with [NotAudited]
            if (propInfo?.GetCustomAttribute<NotAuditedAttribute>() != null)
                continue;

            // Check if property is sensitive
            var isSensitive = DefaultSensitiveProperties.Contains(propName) ||
                              propInfo?.GetCustomAttribute<SensitiveDataAttribute>() != null;

            properties[propName] = isSensitive ? "[REDACTED]" : getValue(prop)?.ToString();
        }

        return JsonSerializer.Serialize(properties, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

    private void ProcessPendingAuditEntries()
    {
        if (_pendingAuditEntries.Count == 0) return;

        try
        {
            using var scope = serviceProvider.CreateScope();
            var auditLedger = scope.ServiceProvider.GetRequiredService<IAuditLedger>();

            // Use synchronous wait since we're in a sync context
            auditLedger.RecordBatchAsync(_pendingAuditEntries.ToList())
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record {Count} audit entries", _pendingAuditEntries.Count);
        }
        finally
        {
            _pendingAuditEntries.Clear();
        }
    }

    private async Task ProcessPendingAuditEntriesAsync()
    {
        if (_pendingAuditEntries.Count == 0) return;

        try
        {
            using var scope = serviceProvider.CreateScope();
            var auditLedger = scope.ServiceProvider.GetRequiredService<IAuditLedger>();
            await auditLedger.RecordBatchAsync(_pendingAuditEntries.ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record {Count} audit entries", _pendingAuditEntries.Count);
        }
        finally
        {
            _pendingAuditEntries.Clear();
        }
    }

    private HttpContext? GetHttpContext()
    {
        try
        {
            var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
            return httpContextAccessor?.HttpContext;
        }
        catch
        {
            return null;
        }
    }

    private static Guid? GetUserId(HttpContext? httpContext)
    {
        if (httpContext?.User.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = httpContext.User.FindFirst("uid")
                          ?? httpContext.User.FindFirst("sub")
                          ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            return userId;

        return null;
    }

    private static string? GetUsername(HttpContext? httpContext)
    {
        if (httpContext?.User.Identity?.IsAuthenticated != true)
            return null;

        return httpContext.User.Identity.Name
               ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
    }
}