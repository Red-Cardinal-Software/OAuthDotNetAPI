using Application.DTOs.Audit;
using Application.Interfaces.Services;
using Domain.Entities.Audit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApi.Integration.Tests.Fixtures;
using Xunit;

namespace WebApi.Integration.Tests.Audit;

/// <summary>
/// Integration tests for the audit ledger functionality.
/// Tests basic audit entry creation, hash chain integrity, and querying.
/// Note: Partitioning and SQL Server Ledger table tests require migrations to be run.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class AuditLedgerTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    [Fact]
    public async Task RecordAsync_ShouldCreateEntryWithHashChain()
    {
        // Arrange
        var entry = new CreateAuditEntryDto
        {
            EventType = AuditEventType.Authentication,
            Action = AuditAction.LoginSuccess,
            UserId = Guid.NewGuid(),
            Username = "testuser@example.com",
            IpAddress = "192.168.1.1",
            Success = true
        };

        // Act
        Guid eventId = Guid.Empty;
        await WithServiceAsync<IAuditLedger>(async auditLedger =>
        {
            var result = await auditLedger.RecordAsync(entry);
            result.Success.Should().BeTrue();
            eventId = result.Data!.EventId;
        });

        // Assert
        await WithDbContextAsync(async db =>
        {
            var savedEntry = await db.AuditLedger
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            savedEntry.Should().NotBeNull();
            savedEntry!.SequenceNumber.Should().BeGreaterThan(0);
            savedEntry.Hash.Should().NotBeNullOrEmpty();
            savedEntry.PreviousHash.Should().NotBeNullOrEmpty();
            savedEntry.EventType.Should().Be(AuditEventType.Authentication);
            savedEntry.Action.Should().Be(AuditAction.LoginSuccess);
            savedEntry.Username.Should().Be("testuser@example.com");
            savedEntry.Success.Should().BeTrue();
        });
    }

    [Fact]
    public async Task RecordAsync_ShouldMaintainHashChainIntegrity()
    {
        // Arrange & Act - Write multiple entries
        var entries = new List<Guid>();
        await WithServiceAsync<IAuditLedger>(async auditLedger =>
        {
            for (int i = 0; i < 3; i++)
            {
                var entry = new CreateAuditEntryDto
                {
                    EventType = AuditEventType.DataChange,
                    Action = AuditAction.DataCreated,
                    EntityType = "TestEntity",
                    EntityId = Guid.NewGuid().ToString(),
                    Success = true
                };
                var result = await auditLedger.RecordAsync(entry);
                result.Success.Should().BeTrue();
                entries.Add(result.Data!.EventId);
            }
        });

        // Assert - Verify hash chain
        await WithDbContextAsync(async db =>
        {
            var savedEntries = await db.AuditLedger
                .Where(e => entries.Contains(e.EventId))
                .OrderBy(e => e.SequenceNumber)
                .ToListAsync();

            savedEntries.Should().HaveCount(3);

            // Each entry's PreviousHash should match the previous entry's Hash
            for (int i = 1; i < savedEntries.Count; i++)
            {
                savedEntries[i].PreviousHash.Should().Be(savedEntries[i - 1].Hash,
                    $"Entry {i} should reference entry {i - 1}'s hash");
            }
        });
    }

    [Fact]
    public async Task VerifyIntegrityAsync_ShouldReturnValidForIntactChain()
    {
        // Arrange - Write some entries and track the sequence range
        long firstSequence = 0;
        long lastSequence = 0;

        await WithServiceAsync<IAuditLedger>(async auditLedger =>
        {
            for (int i = 0; i < 5; i++)
            {
                var entry = new CreateAuditEntryDto
                {
                    EventType = AuditEventType.UserManagement,
                    Action = AuditAction.UserCreated,
                    EntityType = "AppUser",
                    EntityId = Guid.NewGuid().ToString(),
                    Success = true
                };
                var result = await auditLedger.RecordAsync(entry);
                result.Success.Should().BeTrue();

                if (i == 0)
                    firstSequence = result.Data!.SequenceNumber;
                if (i == 4)
                    lastSequence = result.Data!.SequenceNumber;
            }
        });

        // Act & Assert - Verify only the range we created
        await WithServiceAsync<IAuditLedger>(async auditLedger =>
        {
            var result = await auditLedger.VerifyIntegrityAsync(firstSequence, lastSequence);
            result.Success.Should().BeTrue();
            result.Data!.IsValid.Should().BeTrue();
            result.Data.EntriesVerified.Should().Be(5);
        });
    }

    [Fact]
    public async Task QueryAsync_ShouldFilterByUserId()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await WithServiceAsync<IAuditLedger>(async auditLedger =>
        {
            // Create entries for target user
            for (int i = 0; i < 3; i++)
            {
                await auditLedger.RecordAsync(new CreateAuditEntryDto
                {
                    EventType = AuditEventType.Authentication,
                    Action = AuditAction.LoginSuccess,
                    UserId = targetUserId,
                    Success = true
                });
            }

            // Create entries for other user
            for (int i = 0; i < 2; i++)
            {
                await auditLedger.RecordAsync(new CreateAuditEntryDto
                {
                    EventType = AuditEventType.Authentication,
                    Action = AuditAction.LoginSuccess,
                    UserId = otherUserId,
                    Success = true
                });
            }
        });

        // Act & Assert
        await WithServiceAsync<IAuditLedger>(async auditLedger =>
        {
            var result = await auditLedger.QueryAsync(new AuditQueryDto { UserId = targetUserId });

            result.Success.Should().BeTrue();
            result.Data!.Items.Should().HaveCountGreaterThanOrEqualTo(3);
            result.Data.Items.Should().OnlyContain(e => e.UserId == targetUserId);
        });
    }

    [Fact]
    public async Task QueryAsync_ShouldFilterByEntityType()
    {
        // Arrange
        var entityType = "UniqueTestEntity_" + Guid.NewGuid().ToString("N")[..8];

        await WithServiceAsync<IAuditLedger>(async auditLedger =>
        {
            await auditLedger.RecordAsync(new CreateAuditEntryDto
            {
                EventType = AuditEventType.DataChange,
                Action = AuditAction.DataCreated,
                EntityType = entityType,
                EntityId = "123",
                Success = true
            });

            await auditLedger.RecordAsync(new CreateAuditEntryDto
            {
                EventType = AuditEventType.DataChange,
                Action = AuditAction.DataUpdated,
                EntityType = entityType,
                EntityId = "123",
                Success = true
            });
        });

        // Act & Assert
        await WithServiceAsync<IAuditLedger>(async auditLedger =>
        {
            var result = await auditLedger.QueryAsync(new AuditQueryDto { EntityType = entityType });

            result.Success.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items.Should().OnlyContain(e => e.EntityType == entityType);
        });
    }

    [Fact]
    public async Task QueryAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var yesterday = now.AddDays(-1);
        var tomorrow = now.AddDays(1);

        await WithServiceAsync<IAuditLedger>(async auditLedger =>
        {
            await auditLedger.RecordAsync(new CreateAuditEntryDto
            {
                EventType = AuditEventType.SystemEvent,
                Action = AuditAction.ConfigurationChanged,
                Success = true
            });
        });

        // Act & Assert
        await WithServiceAsync<IAuditLedger>(async auditLedger =>
        {
            var result = await auditLedger.QueryAsync(new AuditQueryDto
            {
                FromDate = yesterday,
                ToDate = tomorrow
            });

            result.Success.Should().BeTrue();
            result.Data!.Items.Should().NotBeEmpty();
            result.Data.Items.Should().OnlyContain(e =>
                e.OccurredAt >= yesterday && e.OccurredAt <= tomorrow);
        });
    }

    [Fact]
    public async Task RecordAsync_ShouldRecordFailureReason()
    {
        // Arrange & Act
        Guid eventId = Guid.Empty;
        await WithServiceAsync<IAuditLedger>(async auditLedger =>
        {
            var entry = new CreateAuditEntryDto
            {
                EventType = AuditEventType.Authentication,
                Action = AuditAction.LoginFailed,
                Username = "baduser@example.com",
                IpAddress = "10.0.0.1",
                Success = false,
                FailureReason = "Invalid credentials"
            };
            var result = await auditLedger.RecordAsync(entry);
            result.Success.Should().BeTrue();
            eventId = result.Data!.EventId;
        });

        // Assert
        await WithDbContextAsync(async db =>
        {
            var savedEntry = await db.AuditLedger
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            savedEntry.Should().NotBeNull();
            savedEntry!.Success.Should().BeFalse();
            savedEntry.FailureReason.Should().Be("Invalid credentials");
        });
    }

    [Fact]
    public async Task GetEntityHistoryAsync_ShouldReturnEntityAuditTrail()
    {
        // Arrange
        var entityType = "TestProduct";
        var entityId = Guid.NewGuid().ToString();

        await WithServiceAsync<IAuditLedger>(async auditLedger =>
        {
            await auditLedger.RecordAsync(new CreateAuditEntryDto
            {
                EventType = AuditEventType.DataChange,
                Action = AuditAction.DataCreated,
                EntityType = entityType,
                EntityId = entityId,
                NewValues = "{\"name\": \"Test Product\"}",
                Success = true
            });

            await auditLedger.RecordAsync(new CreateAuditEntryDto
            {
                EventType = AuditEventType.DataChange,
                Action = AuditAction.DataUpdated,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = "{\"name\": \"Test Product\"}",
                NewValues = "{\"name\": \"Updated Product\"}",
                Success = true
            });
        });

        // Act & Assert
        await WithServiceAsync<IAuditLedger>(async auditLedger =>
        {
            var result = await auditLedger.GetEntityHistoryAsync(entityType, entityId);

            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.Should().OnlyContain(e => e.EntityType == entityType && e.EntityId == entityId);
        });
    }

    [Fact]
    public async Task RecordBatchAsync_ShouldCreateMultipleEntries()
    {
        // Arrange
        var entries = new List<CreateAuditEntryDto>
        {
            new()
            {
                EventType = AuditEventType.SecurityEvent,
                Action = AuditAction.AccountLocked,
                UserId = Guid.NewGuid(),
                Success = true
            },
            new()
            {
                EventType = AuditEventType.SecurityEvent,
                Action = AuditAction.AccountUnlocked,
                UserId = Guid.NewGuid(),
                Success = true
            }
        };

        // Act
        List<Guid> createdIds = [];
        await WithServiceAsync<IAuditLedger>(async auditLedger =>
        {
            var result = await auditLedger.RecordBatchAsync(entries);
            result.Success.Should().BeTrue();
            createdIds = result.Data!.Select(e => e.EventId).ToList();
        });

        // Assert
        await WithDbContextAsync(async db =>
        {
            var savedEntries = await db.AuditLedger
                .Where(e => createdIds.Contains(e.EventId))
                .ToListAsync();

            savedEntries.Should().HaveCount(2);
        });
    }
}