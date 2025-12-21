using Application.Interfaces.Services;
using Domain.Entities.Audit;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Application.Tests.ServiceTests;

public class AuditArchiveBackgroundServiceTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<IAuditArchiver> _archiverMock = new();
    private readonly Mock<ILogger<AuditArchiveBackgroundService>> _loggerMock = new();

    public AuditArchiveBackgroundServiceTests()
    {
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IAuditArchiver)))
            .Returns(_archiverMock.Object);
    }

    private AuditArchiveBackgroundService CreateService(AuditArchiveOptions? options = null)
    {
        options ??= new AuditArchiveOptions();
        var optionsMock = new Mock<IOptions<AuditArchiveOptions>>();
        optionsMock.Setup(x => x.Value).Returns(options);

        return new AuditArchiveBackgroundService(
            _scopeFactoryMock.Object,
            _loggerMock.Object,
            optionsMock.Object);
    }

    [Fact]
    public void DefaultOptions_ShouldHaveCorrectValues()
    {
        var options = new AuditArchiveOptions();

        options.Enabled.Should().BeTrue();
        options.CheckInterval.Should().Be(TimeSpan.FromHours(1));
        options.AddPartitionOnDay.Should().Be(25);
        options.ArchiveOnDay.Should().Be(5);
        options.MonthsToKeepBeforeArchive.Should().Be(2);
        options.AutoPurgeAfterArchive.Should().BeTrue();
        options.MinWaitBeforePurge.Should().Be(TimeSpan.FromHours(24));
        options.RetentionPolicy.Should().Be("default");
    }

    [Fact]
    public void Options_ShouldBeConfigurable()
    {
        var options = new AuditArchiveOptions
        {
            Enabled = false,
            CheckInterval = TimeSpan.FromMinutes(30),
            AddPartitionOnDay = 20,
            ArchiveOnDay = 10,
            MonthsToKeepBeforeArchive = 3,
            AutoPurgeAfterArchive = false,
            MinWaitBeforePurge = TimeSpan.FromHours(48),
            RetentionPolicy = "hipaa-7-year"
        };

        options.Enabled.Should().BeFalse();
        options.CheckInterval.Should().Be(TimeSpan.FromMinutes(30));
        options.AddPartitionOnDay.Should().Be(20);
        options.ArchiveOnDay.Should().Be(10);
        options.MonthsToKeepBeforeArchive.Should().Be(3);
        options.AutoPurgeAfterArchive.Should().BeFalse();
        options.MinWaitBeforePurge.Should().Be(TimeSpan.FromHours(48));
        options.RetentionPolicy.Should().Be("hipaa-7-year");
    }

    [Fact]
    public void SectionName_ShouldBeAuditArchive()
    {
        AuditArchiveOptions.SectionName.Should().Be("AuditArchive");
    }
}

public class AuditArchiveResultTests
{
    [Fact]
    public void Succeeded_ShouldCreateSuccessResult()
    {
        var manifest = CreateTestManifest();

        var result = AuditArchiveResult.Succeeded(manifest);

        result.Success.Should().BeTrue();
        result.Manifest.Should().Be(manifest);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Failed_ShouldCreateFailureResult()
    {
        var result = AuditArchiveResult.Failed("Something went wrong");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Something went wrong");
        result.Manifest.Should().BeNull();
    }

    private static AuditArchiveManifest CreateTestManifest()
    {
        return AuditArchiveManifest.Create(
            partitionBoundary: new DateTime(2025, 1, 1),
            firstSequenceNumber: 1,
            lastSequenceNumber: 100,
            recordCount: 100,
            firstRecordHash: "hash1",
            lastRecordHash: "hash2",
            ledgerDigest: "{}",
            archiveUri: "https://example.com/archive",
            archiveBlobHash: "blobhash",
            archiveSizeBytes: 1024,
            archivedBy: "test");
    }
}

public class ArchiveVerificationResultTests
{
    [Fact]
    public void Valid_ShouldCreateValidResult()
    {
        var result = ArchiveVerificationResult.Valid();

        result.IsValid.Should().BeTrue();
        result.BlobIntegrityValid.Should().BeTrue();
        result.HashChainValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Invalid_ShouldCreateInvalidResult()
    {
        var result = ArchiveVerificationResult.Invalid("Hash mismatch");

        result.IsValid.Should().BeFalse();
        result.BlobIntegrityValid.Should().BeFalse();
        result.HashChainValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Hash mismatch");
    }

    [Fact]
    public void Invalid_ShouldAllowPartialValidity()
    {
        var result = ArchiveVerificationResult.Invalid(
            "Chain broken",
            blobValid: true,
            hashValid: false);

        result.IsValid.Should().BeFalse();
        result.BlobIntegrityValid.Should().BeTrue();
        result.HashChainValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Chain broken");
    }
}

public class BlobUploadResultTests
{
    [Fact]
    public void Succeeded_ShouldCreateSuccessResult()
    {
        var result = BlobUploadResult.Succeeded(
            uri: "https://blob.storage/path/file.json",
            contentHash: "sha256hash",
            sizeBytes: 1048576);

        result.Success.Should().BeTrue();
        result.Uri.Should().Be("https://blob.storage/path/file.json");
        result.ContentHash.Should().Be("sha256hash");
        result.SizeBytes.Should().Be(1048576);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Failed_ShouldCreateFailureResult()
    {
        var result = BlobUploadResult.Failed("Upload timeout");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Upload timeout");
        result.Uri.Should().BeNull();
        result.ContentHash.Should().BeNull();
        result.SizeBytes.Should().Be(0);
    }
}