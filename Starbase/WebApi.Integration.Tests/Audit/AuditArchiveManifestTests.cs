using Domain.Entities.Audit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebApi.Integration.Tests.Fixtures;
using Xunit;

namespace WebApi.Integration.Tests.Audit;

/// <summary>
/// Integration tests for the audit archive manifest functionality.
/// Tests manifest creation, retrieval, and integrity verification.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class AuditArchiveManifestTests(SqlServerContainerFixture dbFixture) : IntegrationTestBase(dbFixture)
{
    [Fact]
    public async Task CreateManifest_ShouldPersistToDatabase()
    {
        // Arrange
        var manifest = AuditArchiveManifest.Create(
            partitionBoundary: new DateTime(2025, 1, 1),
            firstSequenceNumber: 1,
            lastSequenceNumber: 1000,
            recordCount: 1000,
            firstRecordHash: "abc123first",
            lastRecordHash: "xyz789last",
            ledgerDigest: "{\"block_id\": 42, \"hash\": \"digest123\"}",
            archiveUri: "test://audit-archive/2025-01.json",
            archiveBlobHash: "blobhash456",
            archiveSizeBytes: 1048576,
            archivedBy: "integration-test",
            retentionPolicy: "test-policy");

        // Act
        await WithDbContextAsync(async db =>
        {
            db.AuditArchiveManifests.Add(manifest);
            await db.SaveChangesAsync();
        });

        // Assert
        await WithDbContextAsync(async db =>
        {
            var saved = await db.AuditArchiveManifests
                .FirstOrDefaultAsync(m => m.Id == manifest.Id);

            saved.Should().NotBeNull();
            saved!.PartitionBoundary.Should().Be(new DateTime(2025, 1, 1));
            saved.FirstSequenceNumber.Should().Be(1);
            saved.LastSequenceNumber.Should().Be(1000);
            saved.RecordCount.Should().Be(1000);
            saved.FirstRecordHash.Should().Be("abc123first");
            saved.LastRecordHash.Should().Be("xyz789last");
            saved.LedgerDigest.Should().Contain("block_id");
            saved.ArchiveUri.Should().Be("test://audit-archive/2025-01.json");
            saved.ArchiveBlobHash.Should().Be("blobhash456");
            saved.ArchiveSizeBytes.Should().Be(1048576);
            saved.ArchivedBy.Should().Be("integration-test");
            saved.RetentionPolicy.Should().Be("test-policy");
            saved.PurgedAt.Should().BeNull();
        });
    }

    [Fact]
    public async Task PartitionBoundary_ShouldHaveUniqueIndex()
    {
        // Arrange
        var boundary = new DateTime(2024, 6, 1);

        var manifest1 = AuditArchiveManifest.Create(
            partitionBoundary: boundary,
            firstSequenceNumber: 1,
            lastSequenceNumber: 100,
            recordCount: 100,
            firstRecordHash: "hash1",
            lastRecordHash: "hash2",
            ledgerDigest: "{}",
            archiveUri: "test://first",
            archiveBlobHash: "blob1",
            archiveSizeBytes: 1000,
            archivedBy: "test");

        var manifest2 = AuditArchiveManifest.Create(
            partitionBoundary: boundary, // Same boundary - should fail
            firstSequenceNumber: 101,
            lastSequenceNumber: 200,
            recordCount: 100,
            firstRecordHash: "hash3",
            lastRecordHash: "hash4",
            ledgerDigest: "{}",
            archiveUri: "test://second",
            archiveBlobHash: "blob2",
            archiveSizeBytes: 1000,
            archivedBy: "test");

        // Act
        await WithDbContextAsync(async db =>
        {
            db.AuditArchiveManifests.Add(manifest1);
            await db.SaveChangesAsync();
        });

        // Assert - Second insert should fail due to unique constraint
        var act = async () => await WithDbContextAsync(async db =>
        {
            db.AuditArchiveManifests.Add(manifest2);
            await db.SaveChangesAsync();
        });

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task MarkPurged_ShouldUpdatePurgedAt()
    {
        // Arrange - Use a unique date based on current time to avoid conflicts with other tests
        var uniqueDate = new DateTime(2020, 1 + (int)(DateTime.UtcNow.Ticks % 12), 1);
        var manifest = AuditArchiveManifest.Create(
            partitionBoundary: uniqueDate,
            firstSequenceNumber: 1,
            lastSequenceNumber: 50,
            recordCount: 50,
            firstRecordHash: "first",
            lastRecordHash: "last",
            ledgerDigest: "{}",
            archiveUri: $"test://purge-test-{uniqueDate:yyyy-MM}",
            archiveBlobHash: "hash",
            archiveSizeBytes: 500,
            archivedBy: "test");

        await WithDbContextAsync(async db =>
        {
            db.AuditArchiveManifests.Add(manifest);
            await db.SaveChangesAsync();
        });

        // Act
        await WithDbContextAsync(async db =>
        {
            var toUpdate = await db.AuditArchiveManifests.FindAsync(manifest.Id);
            toUpdate!.MarkPurged();
            await db.SaveChangesAsync();
        });

        // Assert
        await WithDbContextAsync(async db =>
        {
            var updated = await db.AuditArchiveManifests.FindAsync(manifest.Id);

            updated!.PurgedAt.Should().NotBeNull();
            updated.PurgedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        });
    }

    [Fact]
    public async Task QueryManifests_ShouldFilterByDateRange()
    {
        // Arrange - Create manifests for different months (using 2023 to avoid conflicts with other tests)
        var months = new[] {
            new DateTime(2023, 1, 1),
            new DateTime(2023, 2, 1),
            new DateTime(2023, 3, 1),
            new DateTime(2023, 4, 1)
        };

        await WithDbContextAsync(async db =>
        {
            foreach (var month in months)
            {
                var manifest = AuditArchiveManifest.Create(
                    partitionBoundary: month,
                    firstSequenceNumber: month.Month * 100,
                    lastSequenceNumber: month.Month * 100 + 99,
                    recordCount: 100,
                    firstRecordHash: $"first-{month:yyyy-MM}",
                    lastRecordHash: $"last-{month:yyyy-MM}",
                    ledgerDigest: "{}",
                    archiveUri: $"test://{month:yyyy-MM}",
                    archiveBlobHash: $"hash-{month:yyyy-MM}",
                    archiveSizeBytes: 1000,
                    archivedBy: "test");

                db.AuditArchiveManifests.Add(manifest);
            }
            await db.SaveChangesAsync();
        });

        // Act & Assert - Query for Feb-Mar only
        await WithDbContextAsync(async db =>
        {
            var results = await db.AuditArchiveManifests
                .Where(m => m.PartitionBoundary >= new DateTime(2023, 2, 1) &&
                           m.PartitionBoundary <= new DateTime(2023, 3, 1))
                .OrderBy(m => m.PartitionBoundary)
                .ToListAsync();

            results.Should().HaveCount(2);
            results[0].PartitionBoundary.Should().Be(new DateTime(2023, 2, 1));
            results[1].PartitionBoundary.Should().Be(new DateTime(2023, 3, 1));
        });
    }

    [Fact]
    public async Task VerifyBlobIntegrity_ShouldValidateHash()
    {
        // Arrange
        var manifest = AuditArchiveManifest.Create(
            partitionBoundary: new DateTime(2024, 5, 1),
            firstSequenceNumber: 1,
            lastSequenceNumber: 100,
            recordCount: 100,
            firstRecordHash: "first",
            lastRecordHash: "last",
            ledgerDigest: "{}",
            archiveUri: "test://verify-test",
            archiveBlobHash: "CORRECTHASH123",
            archiveSizeBytes: 1000,
            archivedBy: "test");

        await WithDbContextAsync(async db =>
        {
            db.AuditArchiveManifests.Add(manifest);
            await db.SaveChangesAsync();
        });

        // Act & Assert
        await WithDbContextAsync(async db =>
        {
            var saved = await db.AuditArchiveManifests.FindAsync(manifest.Id);

            // Correct hash (case-insensitive)
            saved!.VerifyBlobIntegrity("correcthash123").Should().BeTrue();
            saved.VerifyBlobIntegrity("CORRECTHASH123").Should().BeTrue();

            // Wrong hash
            saved.VerifyBlobIntegrity("wronghash").Should().BeFalse();
        });
    }

    [Fact]
    public async Task MultipleManifests_ShouldMaintainSequenceConsistency()
    {
        // Arrange - Create manifests with consecutive sequence ranges
        var manifests = new List<AuditArchiveManifest>
        {
            AuditArchiveManifest.Create(
                partitionBoundary: new DateTime(2024, 7, 1),
                firstSequenceNumber: 1,
                lastSequenceNumber: 1000,
                recordCount: 1000,
                firstRecordHash: "genesis",
                lastRecordHash: "july-end",
                ledgerDigest: "{}",
                archiveUri: "test://2024-07",
                archiveBlobHash: "hash1",
                archiveSizeBytes: 10000,
                archivedBy: "test"),

            AuditArchiveManifest.Create(
                partitionBoundary: new DateTime(2024, 8, 1),
                firstSequenceNumber: 1001,
                lastSequenceNumber: 2500,
                recordCount: 1500,
                firstRecordHash: "july-end", // Should match previous last
                lastRecordHash: "august-end",
                ledgerDigest: "{}",
                archiveUri: "test://2024-08",
                archiveBlobHash: "hash2",
                archiveSizeBytes: 15000,
                archivedBy: "test")
        };

        await WithDbContextAsync(async db =>
        {
            db.AuditArchiveManifests.AddRange(manifests);
            await db.SaveChangesAsync();
        });

        // Assert - Verify chain consistency
        await WithDbContextAsync(async db =>
        {
            var saved = await db.AuditArchiveManifests
                .Where(m => m.PartitionBoundary >= new DateTime(2024, 7, 1))
                .OrderBy(m => m.PartitionBoundary)
                .ToListAsync();

            saved.Should().HaveCount(2);

            // Second manifest's first hash should match first manifest's last hash
            saved[1].FirstRecordHash.Should().Be(saved[0].LastRecordHash,
                "Archive chain should be continuous");

            // Sequence numbers should be consecutive
            saved[1].FirstSequenceNumber.Should().Be(saved[0].LastSequenceNumber + 1,
                "Sequence numbers should be consecutive across partitions");
        });
    }
}