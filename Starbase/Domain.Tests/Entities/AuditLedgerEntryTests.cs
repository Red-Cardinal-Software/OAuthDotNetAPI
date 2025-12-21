using Domain.Entities.Audit;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Entities;

public class AuditLedgerEntryTests
{
    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        var entry = new AuditLedgerEntry();

        entry.SequenceNumber.Should().Be(0);
        entry.EventId.Should().NotBe(Guid.Empty);
        entry.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entry.PreviousHash.Should().Be(string.Empty);
        entry.Hash.Should().Be(string.Empty);
        entry.UserId.Should().BeNull();
        entry.Username.Should().BeNull();
        entry.IpAddress.Should().BeNull();
        entry.UserAgent.Should().BeNull();
        entry.CorrelationId.Should().BeNull();
        entry.EventType.Should().Be(default(AuditEventType));
        entry.Action.Should().Be(default(AuditAction));
        entry.EntityType.Should().BeNull();
        entry.EntityId.Should().BeNull();
        entry.OldValues.Should().BeNull();
        entry.NewValues.Should().BeNull();
        entry.AdditionalData.Should().BeNull();
        entry.Success.Should().BeFalse();
        entry.FailureReason.Should().BeNull();
        entry.Dispatched.Should().BeFalse();
        entry.DispatchedAt.Should().BeNull();
    }

    [Fact]
    public void InitProperties_ShouldAllowSettingValues()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var occurredAt = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        var entry = new AuditLedgerEntry
        {
            SequenceNumber = 42,
            EventId = eventId,
            OccurredAt = occurredAt,
            PreviousHash = "prevhash123",
            Hash = "currenthash456",
            UserId = userId,
            Username = "testuser",
            IpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0",
            CorrelationId = "corr-123-456",
            EventType = AuditEventType.Authentication,
            Action = AuditAction.LoginSuccess,
            EntityType = "AppUser",
            EntityId = userId.ToString(),
            OldValues = null,
            NewValues = "{\"lastLogin\":\"2025-06-15\"}",
            AdditionalData = "{\"browser\":\"Chrome\"}",
            Success = true,
            FailureReason = null
        };

        entry.SequenceNumber.Should().Be(42);
        entry.EventId.Should().Be(eventId);
        entry.OccurredAt.Should().Be(occurredAt);
        entry.PreviousHash.Should().Be("prevhash123");
        entry.Hash.Should().Be("currenthash456");
        entry.UserId.Should().Be(userId);
        entry.Username.Should().Be("testuser");
        entry.IpAddress.Should().Be("192.168.1.100");
        entry.UserAgent.Should().Be("Mozilla/5.0");
        entry.CorrelationId.Should().Be("corr-123-456");
        entry.EventType.Should().Be(AuditEventType.Authentication);
        entry.Action.Should().Be(AuditAction.LoginSuccess);
        entry.EntityType.Should().Be("AppUser");
        entry.EntityId.Should().Be(userId.ToString());
        entry.NewValues.Should().Be("{\"lastLogin\":\"2025-06-15\"}");
        entry.AdditionalData.Should().Be("{\"browser\":\"Chrome\"}");
        entry.Success.Should().BeTrue();
    }

    [Fact]
    public void Dispatched_ShouldBeSettable()
    {
        var entry = new AuditLedgerEntry();
        entry.Dispatched.Should().BeFalse();

        entry.Dispatched = true;

        entry.Dispatched.Should().BeTrue();
    }

    [Fact]
    public void DispatchedAt_ShouldBeSettable()
    {
        var entry = new AuditLedgerEntry();
        entry.DispatchedAt.Should().BeNull();

        var dispatchTime = DateTime.UtcNow;
        entry.DispatchedAt = dispatchTime;

        entry.DispatchedAt.Should().Be(dispatchTime);
    }

    [Fact]
    public void FailedEntry_ShouldHaveSuccessFalseAndFailureReason()
    {
        var entry = new AuditLedgerEntry
        {
            SequenceNumber = 1,
            EventType = AuditEventType.Authentication,
            Action = AuditAction.LoginFailed,
            Success = false,
            FailureReason = "Invalid credentials"
        };

        entry.Success.Should().BeFalse();
        entry.FailureReason.Should().Be("Invalid credentials");
    }

    [Fact]
    public void SystemEvent_ShouldHaveNullUserId()
    {
        var entry = new AuditLedgerEntry
        {
            SequenceNumber = 1,
            EventType = AuditEventType.SystemEvent,
            Action = AuditAction.MaintenanceStarted,
            UserId = null,
            Username = null,
            Success = true
        };

        entry.UserId.Should().BeNull();
        entry.Username.Should().BeNull();
        entry.EventType.Should().Be(AuditEventType.SystemEvent);
    }

    [Theory]
    [InlineData(AuditEventType.Authentication)]
    [InlineData(AuditEventType.Authorization)]
    [InlineData(AuditEventType.MfaOperation)]
    [InlineData(AuditEventType.UserManagement)]
    [InlineData(AuditEventType.RoleManagement)]
    [InlineData(AuditEventType.PasswordOperation)]
    [InlineData(AuditEventType.DataAccess)]
    [InlineData(AuditEventType.DataChange)]
    [InlineData(AuditEventType.SecurityEvent)]
    [InlineData(AuditEventType.SystemEvent)]
    public void AllEventTypes_ShouldBeAssignable(AuditEventType eventType)
    {
        var entry = new AuditLedgerEntry
        {
            EventType = eventType
        };

        entry.EventType.Should().Be(eventType);
    }

    [Theory]
    [InlineData(AuditAction.LoginSuccess)]
    [InlineData(AuditAction.LoginFailed)]
    [InlineData(AuditAction.MfaSetupCompleted)]
    [InlineData(AuditAction.PasswordChanged)]
    [InlineData(AuditAction.UserCreated)]
    [InlineData(AuditAction.RoleAssigned)]
    [InlineData(AuditAction.DataUpdated)]
    [InlineData(AuditAction.AccountLocked)]
    [InlineData(AuditAction.ConfigurationChanged)]
    public void VariousActions_ShouldBeAssignable(AuditAction action)
    {
        var entry = new AuditLedgerEntry
        {
            Action = action
        };

        entry.Action.Should().Be(action);
    }

    [Fact]
    public void TwoEntries_ShouldHaveDifferentEventIds()
    {
        var entry1 = new AuditLedgerEntry();
        var entry2 = new AuditLedgerEntry();

        entry1.EventId.Should().NotBe(entry2.EventId);
    }

    [Fact]
    public void IpAddress_ShouldAcceptIpv6()
    {
        var entry = new AuditLedgerEntry
        {
            IpAddress = "2001:0db8:85a3:0000:0000:8a2e:0370:7334"
        };

        entry.IpAddress.Should().Be("2001:0db8:85a3:0000:0000:8a2e:0370:7334");
    }
}