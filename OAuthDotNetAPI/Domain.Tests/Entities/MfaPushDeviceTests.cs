using Domain.Entities.Security;
using Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Entities;

public class MfaPushDeviceTests
{
    private readonly Guid _mfaMethodId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private const string TestDeviceId = "device-123";
    private const string TestDeviceName = "iPhone 14 Pro";
    private const string TestPlatform = "iOS";
    private const string TestPushToken = "apns-token-12345";
    private const string TestPublicKey = "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFA...\n-----END PUBLIC KEY-----";

    #region Creation Tests

    [Fact]
    public void Constructor_ShouldInitializeCorrectly_WithValidData()
    {
        // Act
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);

        // Assert
        device.Id.Should().NotBe(Guid.Empty);
        device.MfaMethodId.Should().Be(_mfaMethodId);
        device.UserId.Should().Be(_userId);
        device.DeviceId.Should().Be(TestDeviceId);
        device.DeviceName.Should().Be(TestDeviceName);
        device.Platform.Should().Be(TestPlatform);
        device.PushToken.Should().Be(TestPushToken);
        device.PublicKey.Should().Be(TestPublicKey);
        device.RegisteredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        device.LastUsedAt.Should().BeNull();
        device.IsActive.Should().BeTrue();
        device.TrustScore.Should().Be(50); // Initial medium trust
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueIds()
    {
        // Act
        var device1 = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);
        var device2 = new MfaPushDevice(_mfaMethodId, _userId, "device-456", "Android Phone", "Android", "fcm-token-456", TestPublicKey);

        // Assert
        device1.Id.Should().NotBe(device2.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenDeviceIdIsInvalid(string? deviceId)
    {
        // Act & Assert
        var act = () => new MfaPushDevice(_mfaMethodId, _userId, deviceId!, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("deviceId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenDeviceNameIsInvalid(string? deviceName)
    {
        // Act & Assert
        var act = () => new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, deviceName!, TestPlatform, TestPushToken, TestPublicKey);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("deviceName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenPlatformIsInvalid(string? platform)
    {
        // Act & Assert
        var act = () => new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, platform!, TestPushToken, TestPublicKey);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("platform");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenPushTokenIsInvalid(string? pushToken)
    {
        // Act & Assert
        var act = () => new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, pushToken!, TestPublicKey);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pushToken");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenPublicKeyIsInvalid(string? publicKey)
    {
        // Act & Assert
        var act = () => new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, publicKey!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("publicKey");
    }

    #endregion

    #region Push Token Update Tests

    [Fact]
    public void UpdatePushToken_ShouldUpdateToken_WhenValidToken()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);
        const string newToken = "new-push-token-67890";

        // Act
        device.UpdatePushToken(newToken);

        // Assert
        device.PushToken.Should().Be(newToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdatePushToken_ShouldThrow_WhenTokenIsInvalid(string? newToken)
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);

        // Act & Assert
        var act = () => device.UpdatePushToken(newToken!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("newToken");
    }

    [Fact]
    public void UpdatePushToken_ShouldAllowMultipleUpdates()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);

        // Act
        device.UpdatePushToken("token1");
        device.UpdatePushToken("token2");
        device.UpdatePushToken("token3");

        // Assert
        device.PushToken.Should().Be("token3");
    }

    #endregion

    #region Successful Use Tests

    [Fact]
    public void RecordSuccessfulUse_ShouldUpdateLastUsedAt()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);

        // Act
        device.RecordSuccessfulUse();

        // Assert
        device.LastUsedAt.Should().NotBeNull();
        device.LastUsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordSuccessfulUse_ShouldIncreaseTrustScore()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);
        var initialTrustScore = device.TrustScore; // Should be 50

        // Act
        device.RecordSuccessfulUse();

        // Assert
        device.TrustScore.Should().Be(initialTrustScore + 5);
    }

    [Fact]
    public void RecordSuccessfulUse_ShouldCapTrustScoreAt100()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);
        
        // Set trust score to 98 using reflection
        SetTrustScore(device, 98);

        // Act - This should increase to 100, not 103
        device.RecordSuccessfulUse();

        // Assert
        device.TrustScore.Should().Be(100);
        
        // Act again - Should stay at 100
        device.RecordSuccessfulUse();
        device.TrustScore.Should().Be(100);
    }

    [Fact]
    public void RecordSuccessfulUse_ShouldUpdateLastUsedAtOnMultipleCalls()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);

        // Act - First use
        device.RecordSuccessfulUse();
        var firstUseTime = device.LastUsedAt;
        
        // Small delay
        Thread.Sleep(10);
        
        // Act - Second use
        device.RecordSuccessfulUse();
        var secondUseTime = device.LastUsedAt;

        // Assert
        secondUseTime.Should().BeAfter(firstUseTime!.Value);
    }

    #endregion

    #region Suspicious Activity Tests

    [Fact]
    public void RecordSuspiciousActivity_ShouldDecreaseTrustScore()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);
        var initialTrustScore = device.TrustScore; // Should be 50

        // Act
        device.RecordSuspiciousActivity();

        // Assert
        device.TrustScore.Should().Be(initialTrustScore - 10);
    }

    [Fact]
    public void RecordSuspiciousActivity_ShouldCapTrustScoreAt0()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);
        
        // Set trust score to 5 using reflection
        SetTrustScore(device, 5);

        // Act - This should decrease to 0, not -5
        device.RecordSuspiciousActivity();

        // Assert
        device.TrustScore.Should().Be(0);
        
        // Act again - Should stay at 0
        device.RecordSuspiciousActivity();
        device.TrustScore.Should().Be(0);
    }

    [Fact]
    public void RecordSuspiciousActivity_ShouldAutoDeactivate_WhenTrustScoreBelow20()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);
        
        // Set trust score to 25
        SetTrustScore(device, 25);

        // Act - This should decrease to 15, triggering auto-deactivation
        device.RecordSuspiciousActivity();

        // Assert
        device.TrustScore.Should().Be(15);
        device.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RecordSuspiciousActivity_ShouldNotAutoDeactivate_WhenTrustScore20OrAbove()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);
        
        // Set trust score to 30
        SetTrustScore(device, 30);

        // Act - This should decrease to 20, but not trigger deactivation
        device.RecordSuspiciousActivity();

        // Assert
        device.TrustScore.Should().Be(20);
        device.IsActive.Should().BeTrue();
    }

    #endregion

    #region Activation/Deactivation Tests

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse_WhenActive()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);

        // Act
        device.Deactivate();

        // Assert
        device.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ShouldThrow_WhenAlreadyInactive()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);
        device.Deactivate(); // First deactivation

        // Act & Assert
        var act = () => device.Deactivate();
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("Device is already inactive");
    }

    [Fact]
    public void Reactivate_ShouldSetIsActiveTrue_WhenInactive()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);
        device.Deactivate();

        // Act
        device.Reactivate();

        // Assert
        device.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reactivate_ShouldResetTrustScoreTo50()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);
        
        // Change trust score and deactivate
        SetTrustScore(device, 75);
        device.Deactivate();

        // Act
        device.Reactivate();

        // Assert
        device.TrustScore.Should().Be(50);
    }

    [Fact]
    public void Reactivate_ShouldThrow_WhenAlreadyActive()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);

        // Act & Assert
        var act = () => device.Reactivate();
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("Device is already active");
    }

    #endregion

    #region Device Name Update Tests

    [Fact]
    public void UpdateDeviceName_ShouldUpdateName_WhenValidName()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);
        const string newName = "My Updated Device";

        // Act
        device.UpdateDeviceName(newName);

        // Assert
        device.DeviceName.Should().Be(newName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDeviceName_ShouldThrow_WhenNameIsInvalid(string? newName)
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);

        // Act & Assert
        var act = () => device.UpdateDeviceName(newName!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("newName");
    }

    [Fact]
    public void UpdateDeviceName_ShouldAllowMultipleUpdates()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);

        // Act
        device.UpdateDeviceName("Name 1");
        device.UpdateDeviceName("Name 2");
        device.UpdateDeviceName("Final Name");

        // Assert
        device.DeviceName.Should().Be("Final Name");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_SuccessfulAuthentication_ShouldIncreaseTrust()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);

        // Act - Multiple successful authentications
        device.RecordSuccessfulUse();
        device.RecordSuccessfulUse();
        device.RecordSuccessfulUse();

        // Assert
        device.TrustScore.Should().Be(65); // 50 + (3 * 5)
        device.LastUsedAt.Should().NotBeNull();
        device.IsActive.Should().BeTrue();
    }

    [Fact]
    public void FullWorkflow_SuspiciousActivity_ShouldDecreaseTrustAndPossiblyDeactivate()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);

        // Act - Multiple suspicious activities
        device.RecordSuspiciousActivity(); // 50 -> 40
        device.RecordSuspiciousActivity(); // 40 -> 30
        device.RecordSuspiciousActivity(); // 30 -> 20
        device.RecordSuspiciousActivity(); // 20 -> 10 (should auto-deactivate)

        // Assert
        device.TrustScore.Should().Be(10);
        device.IsActive.Should().BeFalse(); // Auto-deactivated due to low trust
    }

    [Fact]
    public void FullWorkflow_ReactivationAfterSuspiciousActivity_ShouldResetTrust()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);

        // Act - Cause auto-deactivation
        for (int i = 0; i < 4; i++)
        {
            device.RecordSuspiciousActivity();
        }
        
        device.IsActive.Should().BeFalse();

        // Act - Reactivate
        device.Reactivate();

        // Assert
        device.IsActive.Should().BeTrue();
        device.TrustScore.Should().Be(50); // Reset to medium trust
    }

    [Fact]
    public void FullWorkflow_DeviceManagement_ShouldUpdateCorrectly()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);

        // Act - Update device information
        device.UpdateDeviceName("New Device Name");
        device.UpdatePushToken("new-token-123");
        device.RecordSuccessfulUse();

        // Assert
        device.DeviceName.Should().Be("New Device Name");
        device.PushToken.Should().Be("new-token-123");
        device.LastUsedAt.Should().NotBeNull();
        device.TrustScore.Should().Be(55); // 50 + 5
    }

    [Fact]
    public void FullWorkflow_MixedSuccessAndSuspicious_ShouldBalanceTrustScore()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);

        // Act - Mixed activities
        device.RecordSuccessfulUse(); // +5 -> 55
        device.RecordSuspiciousActivity(); // -10 -> 45
        device.RecordSuccessfulUse(); // +5 -> 50
        device.RecordSuccessfulUse(); // +5 -> 55

        // Assert
        device.TrustScore.Should().Be(55);
        device.IsActive.Should().BeTrue();
        device.LastUsedAt.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void TrustScore_ShouldNotExceed100_WithMultipleSuccessfulUses()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);
        
        // Set trust score to near maximum
        SetTrustScore(device, 95);

        // Act - Multiple successful uses
        for (int i = 0; i < 10; i++)
        {
            device.RecordSuccessfulUse();
        }

        // Assert
        device.TrustScore.Should().Be(100);
    }

    [Fact]
    public void TrustScore_ShouldNotGoBelowZero_WithMultipleSuspiciousActivities()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);
        
        // Set trust score to low value
        SetTrustScore(device, 5);

        // Act - Multiple suspicious activities
        for (int i = 0; i < 5; i++)
        {
            device.RecordSuspiciousActivity();
        }

        // Assert
        device.TrustScore.Should().Be(0);
        device.IsActive.Should().BeFalse(); // Should be auto-deactivated
    }

    [Fact]
    public void AutoDeactivation_ShouldOnlyTrigger_WhenCrossingThreshold()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);
        
        // Set trust score exactly to 20
        SetTrustScore(device, 20);

        // Act - One suspicious activity to go below threshold
        device.RecordSuspiciousActivity();

        // Assert
        device.TrustScore.Should().Be(10);
        device.IsActive.Should().BeFalse();
    }

    [Fact]
    public void StateTransitions_ShouldBeConsistent()
    {
        // Arrange
        var device = new MfaPushDevice(_mfaMethodId, _userId, TestDeviceId, TestDeviceName, TestPlatform, TestPushToken, TestPublicKey);

        // Act & Assert - Deactivate then reactivate multiple times
        device.Deactivate();
        device.IsActive.Should().BeFalse();
        
        device.Reactivate();
        device.IsActive.Should().BeTrue();
        device.TrustScore.Should().Be(50);
        
        device.Deactivate();
        device.IsActive.Should().BeFalse();
        
        device.Reactivate();
        device.IsActive.Should().BeTrue();
        device.TrustScore.Should().Be(50);
    }

    #endregion

    #region Helper Methods

    private static void SetTrustScore(MfaPushDevice device, int trustScore)
    {
        var trustScoreProperty = typeof(MfaPushDevice).GetProperty(nameof(MfaPushDevice.TrustScore))!;
        trustScoreProperty.SetValue(device, trustScore);
    }

    #endregion
}
