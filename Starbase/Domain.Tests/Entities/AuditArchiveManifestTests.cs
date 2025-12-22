using Domain.Entities.Audit;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Entities;

public class AuditArchiveManifestTests
{
    private static AuditArchiveManifest CreateValidManifest(
        DateTime? partitionBoundary = null,
        long firstSequence = 1,
        long lastSequence = 100,
        long recordCount = 100)
    {
        return AuditArchiveManifest.Create(
            partitionBoundary: partitionBoundary ?? new DateTime(2025, 1, 1),
            firstSequenceNumber: firstSequence,
            lastSequenceNumber: lastSequence,
            recordCount: recordCount,
            firstRecordHash: "abc123def456",
            lastRecordHash: "xyz789ghi012",
            ledgerDigest: "{\"block_id\": 1}",
            archiveUri: "https://storage.example.com/audit/2025-01.json",
            archiveBlobHash: "sha256hashofblob",
            archiveSizeBytes: 1024000,
            archivedBy: "system-service",
            retentionPolicy: "90-day-rolling");
    }

    [Fact]
    public void Create_ShouldInitializeAllProperties()
    {
        var boundary = new DateTime(2025, 3, 1);

        var manifest = AuditArchiveManifest.Create(
            partitionBoundary: boundary,
            firstSequenceNumber: 1000,
            lastSequenceNumber: 2000,
            recordCount: 1001,
            firstRecordHash: "firsthash",
            lastRecordHash: "lasthash",
            ledgerDigest: "{\"digest\":\"value\"}",
            archiveUri: "https://blob.storage/path",
            archiveBlobHash: "blobhash123",
            archiveSizeBytes: 5000000,
            archivedBy: "admin@example.com",
            retentionPolicy: "hipaa-7-year");

        manifest.Id.Should().NotBe(Guid.Empty);
        manifest.PartitionBoundary.Should().Be(boundary.Date);
        manifest.FirstSequenceNumber.Should().Be(1000);
        manifest.LastSequenceNumber.Should().Be(2000);
        manifest.RecordCount.Should().Be(1001);
        manifest.FirstRecordHash.Should().Be("firsthash");
        manifest.LastRecordHash.Should().Be("lasthash");
        manifest.LedgerDigest.Should().Be("{\"digest\":\"value\"}");
        manifest.ArchiveUri.Should().Be("https://blob.storage/path");
        manifest.ArchiveBlobHash.Should().Be("blobhash123");
        manifest.ArchiveSizeBytes.Should().Be(5000000);
        manifest.ArchivedBy.Should().Be("admin@example.com");
        manifest.RetentionPolicy.Should().Be("hipaa-7-year");
        manifest.ArchivedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        manifest.PurgedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldNormalizeBoundaryToDateOnly()
    {
        var boundaryWithTime = new DateTime(2025, 6, 15, 14, 30, 45);

        var manifest = AuditArchiveManifest.Create(
            partitionBoundary: boundaryWithTime,
            firstSequenceNumber: 1,
            lastSequenceNumber: 10,
            recordCount: 10,
            firstRecordHash: "hash1",
            lastRecordHash: "hash2",
            ledgerDigest: "{}",
            archiveUri: "https://example.com",
            archiveBlobHash: "hash",
            archiveSizeBytes: 100,
            archivedBy: "test");

        manifest.PartitionBoundary.Should().Be(new DateTime(2025, 6, 15));
        manifest.PartitionBoundary.TimeOfDay.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Create_ShouldAllowNullRetentionPolicy()
    {
        var manifest = AuditArchiveManifest.Create(
            partitionBoundary: new DateTime(2025, 1, 1),
            firstSequenceNumber: 1,
            lastSequenceNumber: 10,
            recordCount: 10,
            firstRecordHash: "hash1",
            lastRecordHash: "hash2",
            ledgerDigest: "{}",
            archiveUri: "https://example.com",
            archiveBlobHash: "hash",
            archiveSizeBytes: 100,
            archivedBy: "test",
            retentionPolicy: null);

        manifest.RetentionPolicy.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenFirstRecordHashIsNull(string? hash)
    {
        var act = () => AuditArchiveManifest.Create(
            partitionBoundary: new DateTime(2025, 1, 1),
            firstSequenceNumber: 1,
            lastSequenceNumber: 10,
            recordCount: 10,
            firstRecordHash: hash!,
            lastRecordHash: "hash2",
            ledgerDigest: "{}",
            archiveUri: "https://example.com",
            archiveBlobHash: "hash",
            archiveSizeBytes: 100,
            archivedBy: "test");

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenLastRecordHashIsNull(string? hash)
    {
        var act = () => AuditArchiveManifest.Create(
            partitionBoundary: new DateTime(2025, 1, 1),
            firstSequenceNumber: 1,
            lastSequenceNumber: 10,
            recordCount: 10,
            firstRecordHash: "hash1",
            lastRecordHash: hash!,
            ledgerDigest: "{}",
            archiveUri: "https://example.com",
            archiveBlobHash: "hash",
            archiveSizeBytes: 100,
            archivedBy: "test");

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenLedgerDigestIsNull(string? digest)
    {
        var act = () => AuditArchiveManifest.Create(
            partitionBoundary: new DateTime(2025, 1, 1),
            firstSequenceNumber: 1,
            lastSequenceNumber: 10,
            recordCount: 10,
            firstRecordHash: "hash1",
            lastRecordHash: "hash2",
            ledgerDigest: digest!,
            archiveUri: "https://example.com",
            archiveBlobHash: "hash",
            archiveSizeBytes: 100,
            archivedBy: "test");

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenArchiveUriIsNull(string? uri)
    {
        var act = () => AuditArchiveManifest.Create(
            partitionBoundary: new DateTime(2025, 1, 1),
            firstSequenceNumber: 1,
            lastSequenceNumber: 10,
            recordCount: 10,
            firstRecordHash: "hash1",
            lastRecordHash: "hash2",
            ledgerDigest: "{}",
            archiveUri: uri!,
            archiveBlobHash: "hash",
            archiveSizeBytes: 100,
            archivedBy: "test");

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenArchiveBlobHashIsNull(string? hash)
    {
        var act = () => AuditArchiveManifest.Create(
            partitionBoundary: new DateTime(2025, 1, 1),
            firstSequenceNumber: 1,
            lastSequenceNumber: 10,
            recordCount: 10,
            firstRecordHash: "hash1",
            lastRecordHash: "hash2",
            ledgerDigest: "{}",
            archiveUri: "https://example.com",
            archiveBlobHash: hash!,
            archiveSizeBytes: 100,
            archivedBy: "test");

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenArchivedByIsNull(string? archivedBy)
    {
        var act = () => AuditArchiveManifest.Create(
            partitionBoundary: new DateTime(2025, 1, 1),
            firstSequenceNumber: 1,
            lastSequenceNumber: 10,
            recordCount: 10,
            firstRecordHash: "hash1",
            lastRecordHash: "hash2",
            ledgerDigest: "{}",
            archiveUri: "https://example.com",
            archiveBlobHash: "hash",
            archiveSizeBytes: 100,
            archivedBy: archivedBy!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenFirstSequenceGreaterThanLast()
    {
        var act = () => AuditArchiveManifest.Create(
            partitionBoundary: new DateTime(2025, 1, 1),
            firstSequenceNumber: 100,
            lastSequenceNumber: 50,
            recordCount: 10,
            firstRecordHash: "hash1",
            lastRecordHash: "hash2",
            ledgerDigest: "{}",
            archiveUri: "https://example.com",
            archiveBlobHash: "hash",
            archiveSizeBytes: 100,
            archivedBy: "test");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*First sequence number cannot be greater than last*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_ShouldThrow_WhenRecordCountIsNotPositive(long recordCount)
    {
        var act = () => AuditArchiveManifest.Create(
            partitionBoundary: new DateTime(2025, 1, 1),
            firstSequenceNumber: 1,
            lastSequenceNumber: 10,
            recordCount: recordCount,
            firstRecordHash: "hash1",
            lastRecordHash: "hash2",
            ledgerDigest: "{}",
            archiveUri: "https://example.com",
            archiveBlobHash: "hash",
            archiveSizeBytes: 100,
            archivedBy: "test");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Record count must be positive*");
    }

    [Fact]
    public void MarkPurged_ShouldSetPurgedAt()
    {
        var manifest = CreateValidManifest();
        manifest.PurgedAt.Should().BeNull();

        manifest.MarkPurged();

        manifest.PurgedAt.Should().NotBeNull();
        manifest.PurgedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkPurged_ShouldThrow_WhenAlreadyPurged()
    {
        var manifest = CreateValidManifest();
        manifest.MarkPurged();

        var act = () => manifest.MarkPurged();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already been purged*");
    }

    [Fact]
    public void VerifyBlobIntegrity_ShouldReturnTrue_WhenHashMatches()
    {
        var manifest = AuditArchiveManifest.Create(
            partitionBoundary: new DateTime(2025, 1, 1),
            firstSequenceNumber: 1,
            lastSequenceNumber: 10,
            recordCount: 10,
            firstRecordHash: "hash1",
            lastRecordHash: "hash2",
            ledgerDigest: "{}",
            archiveUri: "https://example.com",
            archiveBlobHash: "ABC123DEF456",
            archiveSizeBytes: 100,
            archivedBy: "test");

        var result = manifest.VerifyBlobIntegrity("ABC123DEF456");

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyBlobIntegrity_ShouldReturnTrue_WhenHashMatchesCaseInsensitive()
    {
        var manifest = AuditArchiveManifest.Create(
            partitionBoundary: new DateTime(2025, 1, 1),
            firstSequenceNumber: 1,
            lastSequenceNumber: 10,
            recordCount: 10,
            firstRecordHash: "hash1",
            lastRecordHash: "hash2",
            ledgerDigest: "{}",
            archiveUri: "https://example.com",
            archiveBlobHash: "ABC123DEF456",
            archiveSizeBytes: 100,
            archivedBy: "test");

        var result = manifest.VerifyBlobIntegrity("abc123def456");

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyBlobIntegrity_ShouldReturnFalse_WhenHashDoesNotMatch()
    {
        var manifest = AuditArchiveManifest.Create(
            partitionBoundary: new DateTime(2025, 1, 1),
            firstSequenceNumber: 1,
            lastSequenceNumber: 10,
            recordCount: 10,
            firstRecordHash: "hash1",
            lastRecordHash: "hash2",
            ledgerDigest: "{}",
            archiveUri: "https://example.com",
            archiveBlobHash: "ABC123DEF456",
            archiveSizeBytes: 100,
            archivedBy: "test");

        var result = manifest.VerifyBlobIntegrity("WRONG_HASH");

        result.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldAllowEqualFirstAndLastSequence()
    {
        // Single record partition
        var manifest = AuditArchiveManifest.Create(
            partitionBoundary: new DateTime(2025, 1, 1),
            firstSequenceNumber: 42,
            lastSequenceNumber: 42,
            recordCount: 1,
            firstRecordHash: "hash1",
            lastRecordHash: "hash1",
            ledgerDigest: "{}",
            archiveUri: "https://example.com",
            archiveBlobHash: "hash",
            archiveSizeBytes: 100,
            archivedBy: "test");

        manifest.FirstSequenceNumber.Should().Be(42);
        manifest.LastSequenceNumber.Should().Be(42);
        manifest.RecordCount.Should().Be(1);
    }
}