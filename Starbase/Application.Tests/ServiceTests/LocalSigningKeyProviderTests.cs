using Application.Common.Configuration;
using FluentAssertions;
using Infrastructure.Security.SigningKey;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Application.Tests.ServiceTests;

/// <summary>
/// Unit tests for LocalSigningKeyProvider.
/// Verifies the development-mode signing key provider behavior.
/// </summary>
public class LocalSigningKeyProviderTests
{
    private readonly Mock<IOptions<AppOptions>> _appOptionsMock;
    private readonly Mock<IOptions<SigningKeyRotationOptions>> _rotationOptionsMock;
    private readonly Mock<ILogger<LocalSigningKeyProvider>> _loggerMock;

    private const string TestSigningKey = "ThisIsATestSigningKeyThatIsLongEnoughForHmacSha256Algorithm!";

    public LocalSigningKeyProviderTests()
    {
        _appOptionsMock = new Mock<IOptions<AppOptions>>();
        _appOptionsMock.Setup(x => x.Value).Returns(new AppOptions
        {
            JwtSigningKey = TestSigningKey,
            JwtIssuer = "test-issuer",
            JwtAudience = "test-audience"
        });

        _rotationOptionsMock = new Mock<IOptions<SigningKeyRotationOptions>>();
        _rotationOptionsMock.Setup(x => x.Value).Returns(new SigningKeyRotationOptions());

        _loggerMock = new Mock<ILogger<LocalSigningKeyProvider>>();
    }

    [Fact]
    public async Task GetCurrentSigningKeyAsync_ReturnsConfiguredKey()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var keyInfo = await provider.GetCurrentSigningKeyAsync();

        // Assert
        keyInfo.Should().NotBeNull();
        keyInfo.Key.Should().NotBeNull();
        keyInfo.IsPrimary.Should().BeTrue();
        keyInfo.KeyId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCurrentSigningKeyAsync_ReturnsSameKeyOnMultipleCalls()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var keyInfo1 = await provider.GetCurrentSigningKeyAsync();
        var keyInfo2 = await provider.GetCurrentSigningKeyAsync();

        // Assert
        keyInfo1.KeyId.Should().Be(keyInfo2.KeyId);
        keyInfo1.Key.Should().Be(keyInfo2.Key);
    }

    [Fact]
    public async Task GetValidationKeysAsync_ReturnsSingleKey()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var keys = await provider.GetValidationKeysAsync();

        // Assert
        keys.Should().HaveCount(1);
        keys[0].IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task RotateKeyAsync_ThrowsNotSupportedException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.RotateKeyAsync();

        // Assert
        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*LocalSigningKeyProvider does not support key rotation*");
    }

    [Fact]
    public async Task IsRotationDueAsync_AlwaysReturnsFalse()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var isDue = await provider.IsRotationDueAsync();

        // Assert
        isDue.Should().BeFalse("local provider should never indicate rotation is due");
    }

    [Fact]
    public async Task RefreshKeysAsync_CompletesWithoutError()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var act = () => provider.RefreshKeysAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetCurrentSigningKeyAsync_KeyNeverExpires()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var keyInfo = await provider.GetCurrentSigningKeyAsync();

        // Assert
        keyInfo.ExpiresAt.Should().BeNull("local keys should never expire");
    }

    [Fact]
    public void Constructor_GeneratesConsistentKeyId()
    {
        // Arrange & Act - Create two providers with same config
        var provider1 = CreateProvider();
        var provider2 = CreateProvider();

        // Assert - Same key content should produce same key ID
        var keyInfo1 = provider1.GetCurrentSigningKeyAsync().Result;
        var keyInfo2 = provider2.GetCurrentSigningKeyAsync().Result;

        keyInfo1.KeyId.Should().Be(keyInfo2.KeyId,
            "same signing key should produce same key ID for consistency");
    }

    [Fact]
    public void Constructor_DifferentKeysProduceDifferentKeyIds()
    {
        // Arrange
        var differentKeyOptions = new Mock<IOptions<AppOptions>>();
        differentKeyOptions.Setup(x => x.Value).Returns(new AppOptions
        {
            JwtSigningKey = "ACompletelyDifferentSigningKeyThatIsAlsoLongEnough!",
            JwtIssuer = "test-issuer",
            JwtAudience = "test-audience"
        });

        var provider1 = CreateProvider();
        var provider2 = new LocalSigningKeyProvider(
            differentKeyOptions.Object,
            _rotationOptionsMock.Object,
            _loggerMock.Object);

        // Act
        var keyInfo1 = provider1.GetCurrentSigningKeyAsync().Result;
        var keyInfo2 = provider2.GetCurrentSigningKeyAsync().Result;

        // Assert
        keyInfo1.KeyId.Should().NotBe(keyInfo2.KeyId,
            "different signing keys should produce different key IDs");
    }

    [Fact]
    public async Task GetCurrentSigningKeyAsync_KeyIsValidAtCurrentTime()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var keyInfo = await provider.GetCurrentSigningKeyAsync();

        // Assert
        keyInfo.IsValidAt(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    private LocalSigningKeyProvider CreateProvider()
    {
        return new LocalSigningKeyProvider(
            _appOptionsMock.Object,
            _rotationOptionsMock.Object,
            _loggerMock.Object);
    }
}