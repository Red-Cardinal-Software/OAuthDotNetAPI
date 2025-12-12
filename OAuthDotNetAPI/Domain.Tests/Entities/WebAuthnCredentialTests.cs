using Domain.Entities.Security;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Entities;

public class WebAuthnCredentialTests
{
    private readonly Guid _mfaMethodId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private const string TestCredentialId = "base64url-credential-id-12345";
    private const string TestPublicKey = "base64url-public-key-67890";
    private const uint TestSignCount = 42;
    private const string TestName = "My Security Key";
    private const string TestAttestationType = "basic";
    private const string TestAaguid = "12345678-1234-5678-9abc-def123456789";
    private const string TestIpAddress = "192.168.1.1";
    private const string TestUserAgent = "Mozilla/5.0 Test Browser";

    #region Creation Tests

    [Fact]
    public void Create_ShouldInitializeCorrectly_WithRequiredData()
    {
        // Arrange
        var transports = new[] { AuthenticatorTransport.Usb, AuthenticatorTransport.Nfc };

        // Act
        var credential = WebAuthnCredential.Create(
            _mfaMethodId,
            _userId,
            TestCredentialId,
            TestPublicKey,
            TestSignCount,
            AuthenticatorType.CrossPlatform,
            transports,
            true);

        // Assert
        credential.Id.Should().NotBe(Guid.Empty);
        credential.MfaMethodId.Should().Be(_mfaMethodId);
        credential.UserId.Should().Be(_userId);
        credential.CredentialId.Should().Be(TestCredentialId);
        credential.PublicKey.Should().Be(TestPublicKey);
        credential.SignCount.Should().Be(TestSignCount);
        credential.AuthenticatorType.Should().Be(AuthenticatorType.CrossPlatform);
        credential.Transports.Should().BeEquivalentTo(transports);
        credential.SupportsUserVerification.Should().BeTrue();
        credential.Name.Should().BeNull();
        credential.AttestationType.Should().BeNull();
        credential.Aaguid.Should().BeNull();
        credential.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        credential.LastUsedAt.Should().BeNull();
        credential.IsActive.Should().BeTrue();
        credential.RegistrationIpAddress.Should().BeNull();
        credential.RegistrationUserAgent.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldInitializeCorrectly_WithAllOptionalData()
    {
        // Arrange
        var transports = new[] { AuthenticatorTransport.Internal };

        // Act
        var credential = WebAuthnCredential.Create(
            _mfaMethodId,
            _userId,
            TestCredentialId,
            TestPublicKey,
            TestSignCount,
            AuthenticatorType.Platform,
            transports,
            false,
            TestName,
            TestAttestationType,
            TestAaguid,
            TestIpAddress,
            TestUserAgent);

        // Assert
        credential.Name.Should().Be(TestName);
        credential.AttestationType.Should().Be(TestAttestationType);
        credential.Aaguid.Should().Be(TestAaguid);
        credential.RegistrationIpAddress.Should().Be(TestIpAddress);
        credential.RegistrationUserAgent.Should().Be(TestUserAgent);
        credential.AuthenticatorType.Should().Be(AuthenticatorType.Platform);
        credential.SupportsUserVerification.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Act
        var credential1 = WebAuthnCredential.Create(_mfaMethodId, _userId, "cred1", TestPublicKey, 1, AuthenticatorType.Platform, [], true);
        var credential2 = WebAuthnCredential.Create(_mfaMethodId, _userId, "cred2", TestPublicKey, 1, AuthenticatorType.Platform, [], true);

        // Assert
        credential1.Id.Should().NotBe(credential2.Id);
    }

    [Fact]
    public void Create_ShouldHandleNullTransports()
    {
        // Act
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, null!, true);

        // Assert
        credential.Transports.Should().NotBeNull();
        credential.Transports.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Create_ShouldThrow_WhenMfaMethodIdIsInvalid(string methodIdString)
    {
        // Arrange
        var methodId = string.IsNullOrEmpty(methodIdString) ? Guid.Empty : Guid.Parse(methodIdString);

        // Act & Assert
        var act = () => WebAuthnCredential.Create(methodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("mfaMethodId")
            .WithMessage("MFA method ID cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Create_ShouldThrow_WhenUserIdIsInvalid(string userIdString)
    {
        // Arrange
        var userId = string.IsNullOrEmpty(userIdString) ? Guid.Empty : Guid.Parse(userIdString);

        // Act & Assert
        var act = () => WebAuthnCredential.Create(_mfaMethodId, userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("userId")
            .WithMessage("User ID cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenCredentialIdIsInvalid(string? credentialId)
    {
        // Act & Assert
        var act = () => WebAuthnCredential.Create(_mfaMethodId, _userId, credentialId!, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("credentialId")
            .WithMessage("Credential ID cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenPublicKeyIsInvalid(string? publicKey)
    {
        // Act & Assert
        var act = () => WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, publicKey!, TestSignCount, AuthenticatorType.Platform, [], true);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("publicKey")
            .WithMessage("Public key cannot be empty*");
    }

    #endregion

    #region Sign Count Tests

    [Fact]
    public void UpdateSignCount_ShouldUpdateCount_WhenCountIncreases()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, 10, AuthenticatorType.Platform, [], true);

        // Act
        var result = credential.UpdateSignCount(15);

        // Assert
        result.Should().BeTrue();
        credential.SignCount.Should().Be(15);
        credential.LastUsedAt.Should().NotBeNull();
        credential.LastUsedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateSignCount_ShouldUpdateCount_WhenCountStaysSame()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, 10, AuthenticatorType.Platform, [], true);

        // Act
        var result = credential.UpdateSignCount(10);

        // Assert
        result.Should().BeTrue();
        credential.SignCount.Should().Be(10);
        credential.LastUsedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateSignCount_ShouldReturnFalse_WhenCountDecreases()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, 10, AuthenticatorType.Platform, [], true);

        // Act
        var result = credential.UpdateSignCount(5);

        // Assert
        result.Should().BeFalse();
        credential.SignCount.Should().Be(10); // Should not change
        credential.LastUsedAt.Should().BeNull(); // Should not update
    }

    [Fact]
    public void UpdateSignCount_ShouldHandleZeroToNonZero()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, 0, AuthenticatorType.Platform, [], true);

        // Act
        var result = credential.UpdateSignCount(1);

        // Assert
        result.Should().BeTrue();
        credential.SignCount.Should().Be(1);
    }

    [Fact]
    public void UpdateSignCount_ShouldHandleMaxValues()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, uint.MaxValue - 1, AuthenticatorType.Platform, [], true);

        // Act
        var result = credential.UpdateSignCount(uint.MaxValue);

        // Assert
        result.Should().BeTrue();
        credential.SignCount.Should().Be(uint.MaxValue);
    }

    #endregion

    #region Activation Tests

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);

        // Act
        credential.Deactivate();

        // Assert
        credential.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);
        credential.Deactivate();

        // Act
        credential.Activate();

        // Assert
        credential.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldWork_WhenAlreadyInactive()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);
        credential.Deactivate();

        // Act & Assert - Should not throw
        var act = () => credential.Deactivate();
        act.Should().NotThrow();
        credential.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldWork_WhenAlreadyActive()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);

        // Act & Assert - Should not throw
        var act = () => credential.Activate();
        act.Should().NotThrow();
        credential.IsActive.Should().BeTrue();
    }

    #endregion

    #region Name Update Tests

    [Fact]
    public void UpdateName_ShouldUpdateName_WithValidString()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);

        // Act
        credential.UpdateName("New Credential Name");

        // Assert
        credential.Name.Should().Be("New Credential Name");
    }

    [Fact]
    public void UpdateName_ShouldTrimWhitespace()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);

        // Act
        credential.UpdateName("  Credential Name  ");

        // Assert
        credential.Name.Should().Be("Credential Name");
    }

    [Fact]
    public void UpdateName_ShouldSetNull_WhenPassedNull()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true, "Initial Name");

        // Act
        credential.UpdateName(null);

        // Assert
        credential.Name.Should().BeNull();
    }

    [Fact]
    public void UpdateName_ShouldHandleEmptyString()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);

        // Act
        credential.UpdateName("");

        // Assert
        credential.Name.Should().Be("");
    }

    [Fact]
    public void UpdateName_ShouldHandleWhitespaceOnlyString()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);

        // Act
        credential.UpdateName("   ");

        // Assert
        credential.Name.Should().Be("");
    }

    #endregion

    #region Record Usage Tests

    [Fact]
    public void RecordUsage_ShouldUpdateLastUsedAt()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);

        // Act
        credential.RecordUsage();

        // Assert
        credential.LastUsedAt.Should().NotBeNull();
        credential.LastUsedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordUsage_ShouldUpdateLastUsedAt_OnMultipleCalls()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);

        // Act - First use
        credential.RecordUsage();
        var firstUseTime = credential.LastUsedAt;

        // Small delay
        Thread.Sleep(10);

        // Act - Second use
        credential.RecordUsage();
        var secondUseTime = credential.LastUsedAt;

        // Assert
        secondUseTime.Should().BeAfter(firstUseTime!.Value);
    }

    #endregion

    #region CanAuthenticate Tests

    [Fact]
    public void CanAuthenticate_ShouldReturnTrue_WhenActiveAndValid()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);

        // Act
        var result = credential.CanAuthenticate();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanAuthenticate_ShouldReturnFalse_WhenInactive()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);
        credential.Deactivate();

        // Act
        var result = credential.CanAuthenticate();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanAuthenticate_ShouldReturnFalse_WhenCredentialIdEmpty()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, "valid-cred-id", TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);

        // Use reflection to set empty credential ID (simulating corrupted data)
        var credentialIdProperty = typeof(WebAuthnCredential).GetProperty(nameof(WebAuthnCredential.CredentialId))!;
        credentialIdProperty.SetValue(credential, "");

        // Act
        var result = credential.CanAuthenticate();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanAuthenticate_ShouldReturnFalse_WhenPublicKeyEmpty()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, "valid-public-key", TestSignCount, AuthenticatorType.Platform, [], true);

        // Use reflection to set empty public key (simulating corrupted data)
        var publicKeyProperty = typeof(WebAuthnCredential).GetProperty(nameof(WebAuthnCredential.PublicKey))!;
        publicKeyProperty.SetValue(credential, "");

        // Act
        var result = credential.CanAuthenticate();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Authenticator Type Tests

    [Theory]
    [InlineData(AuthenticatorType.Platform)]
    [InlineData(AuthenticatorType.CrossPlatform)]
    public void Create_ShouldAccept_AllAuthenticatorTypes(AuthenticatorType authenticatorType)
    {
        // Act
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, authenticatorType, [], true);

        // Assert
        credential.AuthenticatorType.Should().Be(authenticatorType);
    }

    #endregion

    #region Transport Tests

    [Fact]
    public void Create_ShouldAccept_AllTransportTypes()
    {
        // Arrange
        var allTransports = new[]
        {
            AuthenticatorTransport.Usb,
            AuthenticatorTransport.Nfc,
            AuthenticatorTransport.Ble,
            AuthenticatorTransport.Internal,
            AuthenticatorTransport.Hybrid
        };

        // Act
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.CrossPlatform, allTransports, true);

        // Assert
        credential.Transports.Should().BeEquivalentTo(allTransports);
    }

    [Fact]
    public void Create_ShouldAccept_EmptyTransports()
    {
        // Act
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);

        // Assert
        credential.Transports.Should().NotBeNull();
        credential.Transports.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldAccept_SingleTransport()
    {
        // Arrange
        var transports = new[] { AuthenticatorTransport.Internal };

        // Act
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, transports, true);

        // Assert
        credential.Transports.Should().BeEquivalentTo(transports);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_Authentication_ShouldUpdateCorrectly()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, 0, AuthenticatorType.Platform, [AuthenticatorTransport.Internal], true, TestName);

        // Act - Simulate successful authentication
        var signCountResult = credential.UpdateSignCount(1);
        credential.RecordUsage();

        // Assert
        signCountResult.Should().BeTrue();
        credential.SignCount.Should().Be(1);
        credential.LastUsedAt.Should().NotBeNull();
        credential.CanAuthenticate().Should().BeTrue();
    }

    [Fact]
    public void FullWorkflow_SuspiciousSignCount_ShouldDetectCloning()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, 100, AuthenticatorType.CrossPlatform, [AuthenticatorTransport.Usb], true);

        // Act - Simulate potential cloning (sign count goes backwards)
        var result = credential.UpdateSignCount(50);

        // Assert
        result.Should().BeFalse();
        credential.SignCount.Should().Be(100); // Should not change
        credential.LastUsedAt.Should().BeNull(); // Should not update
    }

    [Fact]
    public void FullWorkflow_CredentialManagement_ShouldWork()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);

        // Act - Update name and deactivate
        credential.UpdateName("My Updated Security Key");
        credential.Deactivate();

        // Assert
        credential.Name.Should().Be("My Updated Security Key");
        credential.IsActive.Should().BeFalse();
        credential.CanAuthenticate().Should().BeFalse();

        // Act - Reactivate
        credential.Activate();

        // Assert
        credential.IsActive.Should().BeTrue();
        credential.CanAuthenticate().Should().BeTrue();
    }

    [Fact]
    public void FullWorkflow_MultipleAuthentications_ShouldTrackUsage()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, 1, AuthenticatorType.CrossPlatform, [AuthenticatorTransport.Nfc, AuthenticatorTransport.Usb], true);

        // Act - Multiple successful authentications
        credential.UpdateSignCount(2);
        var firstUseTime = credential.LastUsedAt;

        Thread.Sleep(10);

        credential.UpdateSignCount(3);
        var secondUseTime = credential.LastUsedAt;

        credential.RecordUsage(); // Manual usage recording
        var thirdUseTime = credential.LastUsedAt;

        // Assert
        credential.SignCount.Should().Be(3);
        thirdUseTime.Should().BeAfter(secondUseTime!.Value);
        secondUseTime.Should().BeAfter(firstUseTime!.Value);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SignCount_ShouldHandleOverflow()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, uint.MaxValue, AuthenticatorType.Platform, [], true);

        // Act - Try to update with same max value (some authenticators don't increment)
        var result = credential.UpdateSignCount(uint.MaxValue);

        // Assert
        result.Should().BeTrue();
        credential.SignCount.Should().Be(uint.MaxValue);
    }

    [Fact]
    public void StateTransitions_ShouldBeIdempotent()
    {
        // Arrange
        var credential = WebAuthnCredential.Create(_mfaMethodId, _userId, TestCredentialId, TestPublicKey, TestSignCount, AuthenticatorType.Platform, [], true);

        // Act - Multiple deactivations/activations
        credential.Deactivate();
        credential.Deactivate();
        credential.Activate();
        credential.Activate();

        // Assert
        credential.IsActive.Should().BeTrue();
        credential.CanAuthenticate().Should().BeTrue();
    }

    [Fact]
    public void UserVerification_ShouldBePreserved()
    {
        // Act
        var credentialWithUV = WebAuthnCredential.Create(_mfaMethodId, _userId, "cred1", TestPublicKey, 1, AuthenticatorType.Platform, [], true);
        var credentialWithoutUV = WebAuthnCredential.Create(_mfaMethodId, _userId, "cred2", TestPublicKey, 1, AuthenticatorType.Platform, [], false);

        // Assert
        credentialWithUV.SupportsUserVerification.Should().BeTrue();
        credentialWithoutUV.SupportsUserVerification.Should().BeFalse();
    }

    [Fact]
    public void RegistrationMetadata_ShouldBePreserved()
    {
        // Act
        var credential = WebAuthnCredential.Create(
            _mfaMethodId,
            _userId,
            TestCredentialId,
            TestPublicKey,
            TestSignCount,
            AuthenticatorType.Platform,
            [],
            true,
            TestName,
            TestAttestationType,
            TestAaguid,
            TestIpAddress,
            TestUserAgent);

        // Assert
        credential.AttestationType.Should().Be(TestAttestationType);
        credential.Aaguid.Should().Be(TestAaguid);
        credential.RegistrationIpAddress.Should().Be(TestIpAddress);
        credential.RegistrationUserAgent.Should().Be(TestUserAgent);
    }

    #endregion
}
