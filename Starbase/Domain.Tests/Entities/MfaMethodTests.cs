using Domain.Entities.Security;
using FluentAssertions;
using Xunit;
using System.Text.Json;

namespace Domain.Tests.Entities;

public class MfaMethodTests
{
    private readonly Guid _userId = Guid.NewGuid();

    #region TOTP Creation Tests

    [Fact]
    public void CreateTotp_ShouldInitializeCorrectly_WithValidData()
    {
        // Arrange
        const string secret = "JBSWY3DPEHPK3PXP";
        const string name = "My Authenticator";

        // Act
        var method = MfaMethod.CreateTotp(_userId, secret, name);

        // Assert
        method.Id.Should().NotBe(Guid.Empty);
        method.UserId.Should().Be(_userId);
        method.Type.Should().Be(MfaType.Totp);
        method.Secret.Should().Be(secret);
        method.Name.Should().Be(name);
        method.IsEnabled.Should().BeFalse(); // Must be verified first
        method.IsDefault.Should().BeFalse();
        method.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        method.VerifiedAt.Should().BeNull();
        method.LastUsedAt.Should().BeNull();
        method.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));

        // Verify metadata contains TOTP configuration
        method.Metadata.Should().NotBeNullOrEmpty();
        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(method.Metadata!);
        metadata!.Should().ContainKey("Algorithm");
        metadata.Should().ContainKey("Digits");
        metadata.Should().ContainKey("Period");
    }

    [Fact]
    public void CreateTotp_ShouldUseDefaultName_WhenNotProvided()
    {
        // Act
        var method = MfaMethod.CreateTotp(_userId, "JBSWY3DPEHPK3PXP");

        // Assert
        method.Name.Should().Be("Authenticator App");
    }

    [Theory]
    [InlineData("")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void CreateTotp_ShouldThrow_WhenUserIdIsInvalid(string userIdString)
    {
        // Arrange
        var userId = string.IsNullOrEmpty(userIdString) ? Guid.Empty : Guid.Parse(userIdString);

        // Act & Assert
        var act = () => MfaMethod.CreateTotp(userId, "secret");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("userId")
            .WithMessage("User ID cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateTotp_ShouldThrow_WhenSecretIsInvalid(string? secret)
    {
        // Act & Assert
        var act = () => MfaMethod.CreateTotp(_userId, secret!);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("secret")
            .WithMessage("Secret cannot be empty*");
    }

    #endregion

    #region WebAuthn Creation Tests

    [Fact]
    public void CreateWebAuthn_ShouldInitializeCorrectly_WithValidData()
    {
        // Arrange
        const string credentialId = "credential-123";
        const string publicKey = "public-key-data";
        const string deviceName = "YubiKey 5";

        // Act
        var method = MfaMethod.CreateWebAuthn(_userId, credentialId, publicKey, deviceName);

        // Assert
        method.Id.Should().NotBe(Guid.Empty);
        method.UserId.Should().Be(_userId);
        method.Type.Should().Be(MfaType.WebAuthn);
        method.Secret.Should().Be(credentialId);
        method.Name.Should().Be(deviceName);
        method.IsEnabled.Should().BeTrue(); // WebAuthn is verified during registration
        method.IsDefault.Should().BeFalse();
        method.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        method.VerifiedAt.Should().NotBeNull();
        method.VerifiedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));

        // Verify metadata contains WebAuthn configuration
        method.Metadata.Should().NotBeNullOrEmpty();
        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(method.Metadata!);
        metadata!.Should().ContainKey("PublicKey");
        metadata.Should().ContainKey("Counter");
        metadata.Should().ContainKey("DeviceName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void CreateWebAuthn_ShouldThrow_WhenUserIdIsInvalid(string userIdString)
    {
        // Arrange
        var userId = string.IsNullOrEmpty(userIdString) ? Guid.Empty : Guid.Parse(userIdString);

        // Act & Assert
        var act = () => MfaMethod.CreateWebAuthn(userId, "cred", "key", "device");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("userId")
            .WithMessage("User ID cannot be empty*");
    }

    #endregion

    #region Push Creation Tests

    [Fact]
    public void CreatePush_ShouldInitializeCorrectly_WithValidData()
    {
        // Arrange
        const string name = "My Mobile App";

        // Act
        var method = MfaMethod.CreatePush(_userId, name);

        // Assert
        method.Id.Should().NotBe(Guid.Empty);
        method.UserId.Should().Be(_userId);
        method.Type.Should().Be(MfaType.Push);
        method.Secret.Should().BeNull(); // Push doesn't need a stored secret
        method.Name.Should().Be(name);
        method.IsEnabled.Should().BeFalse(); // Must have at least one device
        method.IsDefault.Should().BeFalse();

        // Verify metadata contains device list
        method.Metadata.Should().NotBeNullOrEmpty();
        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(method.Metadata!);
        metadata!.Should().ContainKey("Devices");
    }

    [Fact]
    public void CreatePush_ShouldUseDefaultName_WhenNotProvided()
    {
        // Act
        var method = MfaMethod.CreatePush(_userId, null!);

        // Assert
        method.Name.Should().Be("Push Notifications");
    }

    #endregion

    #region Email Creation Tests

    [Fact]
    public void CreateEmail_ShouldInitializeCorrectly_WithValidData()
    {
        // Arrange
        const string email = "user@example.com";

        // Act
        var method = MfaMethod.CreateEmail(_userId, email);

        // Assert
        method.Id.Should().NotBe(Guid.Empty);
        method.UserId.Should().Be(_userId);
        method.Type.Should().Be(MfaType.Email);
        method.Secret.Should().BeNull(); // Email doesn't need a stored secret
        method.Name.Should().Be($"Email ({email})");
        method.IsEnabled.Should().BeFalse(); // Must be verified first
        method.IsDefault.Should().BeFalse();

        // Verify metadata contains email configuration
        method.Metadata.Should().NotBeNullOrEmpty();
        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(method.Metadata!);
        metadata!.Should().ContainKey("EmailAddress");
        metadata.Should().ContainKey("Verified");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateEmail_ShouldThrow_WhenEmailIsInvalid(string? email)
    {
        // Act & Assert
        var act = () => MfaMethod.CreateEmail(_userId, email!);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("email")
            .WithMessage("Email cannot be empty*");
    }

    #endregion

    #region Verification Tests

    [Fact]
    public void Verify_ShouldEnableMethod_WhenNotAlreadyVerified()
    {
        // Arrange
        var method = MfaMethod.CreateTotp(_userId, "secret");

        // Act
        method.Verify();

        // Assert
        method.IsEnabled.Should().BeTrue();
        method.VerifiedAt.Should().NotBeNull();
        method.VerifiedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        method.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Verify_ShouldThrow_WhenAlreadyVerified()
    {
        // Arrange
        var method = MfaMethod.CreateWebAuthn(_userId, "cred", "key", "device"); // Already verified

        // Act & Assert
        var act = () => method.Verify();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("MFA method is already verified");
    }

    #endregion

    #region Default Status Tests

    [Fact]
    public void SetAsDefault_ShouldUpdateDefaultStatus()
    {
        // Arrange
        var method = MfaMethod.CreateTotp(_userId, "secret");
        var originalUpdatedAt = method.UpdatedAt;

        // Act
        method.SetAsDefault();

        // Assert
        method.IsDefault.Should().BeTrue();
        method.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public void RemoveDefault_ShouldUpdateDefaultStatus()
    {
        // Arrange
        var method = MfaMethod.CreateTotp(_userId, "secret");
        method.SetAsDefault();
        var originalUpdatedAt = method.UpdatedAt;

        // Act
        method.RemoveDefault();

        // Assert
        method.IsDefault.Should().BeFalse();
        method.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    #endregion

    #region Usage Tracking Tests

    [Fact]
    public void RecordUsage_ShouldUpdateTimestamps()
    {
        // Arrange
        var method = MfaMethod.CreateTotp(_userId, "secret");
        var originalUpdatedAt = method.UpdatedAt;

        // Act
        method.RecordUsage();

        // Assert
        method.LastUsedAt.Should().NotBeNull();
        method.LastUsedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        method.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    #endregion

    #region Disable Tests

    [Fact]
    public void Disable_ShouldDisableMethodAndRemoveDefault()
    {
        // Arrange
        var method = MfaMethod.CreateWebAuthn(_userId, "cred", "key", "device");
        method.SetAsDefault();
        var originalUpdatedAt = method.UpdatedAt;

        // Act
        method.Disable();

        // Assert
        method.IsEnabled.Should().BeFalse();
        method.IsDefault.Should().BeFalse();
        method.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    #endregion

    #region Name Update Tests

    [Fact]
    public void UpdateName_ShouldUpdateNameAndTimestamp()
    {
        // Arrange
        var method = MfaMethod.CreateTotp(_userId, "secret");
        const string newName = "Updated Authenticator";
        var originalUpdatedAt = method.UpdatedAt;

        // Act
        method.UpdateName(newName);

        // Assert
        method.Name.Should().Be(newName);
        method.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateName_ShouldThrow_WhenNameIsInvalid(string? name)
    {
        // Arrange
        var method = MfaMethod.CreateTotp(_userId, "secret");

        // Act & Assert
        var act = () => method.UpdateName(name!);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name")
            .WithMessage("Name cannot be empty*");
    }

    #endregion

    #region Recovery Code Tests

    [Fact]
    public void SetRecoveryCodes_ShouldThrow_WhenNull()
    {
        // Arrange
        var method = MfaMethod.CreateTotp(_userId, "secret");

        // Act & Assert
        var act = () => method.SetRecoveryCodes(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("newRecoveryCodes");
    }

    [Fact]
    public void GetUnusedRecoveryCodeCount_ShouldReturnCorrectCount_WhenNoRecoveryCodes()
    {
        // Arrange
        var method = MfaMethod.CreateTotp(_userId, "secret");

        // Act & Assert
        method.GetUnusedRecoveryCodeCount().Should().Be(0);
    }

    [Fact]
    public void GetUnusedRecoveryCodes_ShouldReturnEmptyList_WhenNoRecoveryCodes()
    {
        // Arrange
        var method = MfaMethod.CreateTotp(_userId, "secret");

        // Act & Assert
        method.GetUnusedRecoveryCodes().Should().BeEmpty();
    }

    [Fact]
    public void TryUseRecoveryCode_ShouldReturnFalse_WhenCodeNotFound()
    {
        // Arrange
        var method = MfaMethod.CreateTotp(_userId, "secret");

        // Act & Assert
        method.TryUseRecoveryCode(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void GetNewRecoveryCodes_ShouldUpdateTimestamp()
    {
        // Arrange
        var method = MfaMethod.CreateTotp(_userId, "secret");
        var originalUpdatedAt = method.UpdatedAt;

        // Act
        var codes = method.GetNewRecoveryCodes();

        // Assert
        codes.Should().BeEmpty(); // No recovery codes set
        method.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    #endregion

    #region Setup Verification Code Tests (Email MFA)

    [Fact]
    public void StoreSetupVerificationCode_ShouldStoreCodeInMetadata_ForEmailMfa()
    {
        // Arrange
        var method = MfaMethod.CreateEmail(_userId, "user@example.com");
        const string hashedCode = "hashed-verification-code";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);

        // Act
        method.StoreSetupVerificationCode(hashedCode, expiresAt);

        // Assert
        var retrievedCode = method.GetSetupVerificationCode();
        retrievedCode.Should().Be(hashedCode);
    }

    [Fact]
    public void StoreSetupVerificationCode_ShouldThrow_ForNonEmailMfa()
    {
        // Arrange
        var method = MfaMethod.CreateTotp(_userId, "secret");

        // Act & Assert
        var act = () => method.StoreSetupVerificationCode("code", DateTimeOffset.UtcNow);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Setup verification codes are only for email MFA");
    }

    [Fact]
    public void StoreSetupVerificationCode_ShouldThrow_ForAlreadyVerifiedMethod()
    {
        // Arrange
        var method = MfaMethod.CreateEmail(_userId, "user@example.com");
        method.Verify();

        // Act & Assert
        var act = () => method.StoreSetupVerificationCode("code", DateTimeOffset.UtcNow);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot store setup code for already verified method");
    }

    [Fact]
    public void GetSetupVerificationCode_ShouldReturnNull_WhenCodeExpired()
    {
        // Arrange
        var method = MfaMethod.CreateEmail(_userId, "user@example.com");
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(-1); // Expired
        method.StoreSetupVerificationCode("code", expiresAt);

        // Act & Assert
        method.GetSetupVerificationCode().Should().BeNull();
    }

    [Fact]
    public void GetSetupVerificationCode_ShouldReturnNull_ForNonEmailMfa()
    {
        // Arrange
        var method = MfaMethod.CreateTotp(_userId, "secret");

        // Act & Assert
        method.GetSetupVerificationCode().Should().BeNull();
    }

    [Fact]
    public void ClearSetupVerificationCode_ShouldRemoveCodeFromMetadata()
    {
        // Arrange
        var method = MfaMethod.CreateEmail(_userId, "user@example.com");
        method.StoreSetupVerificationCode("code", DateTimeOffset.UtcNow.AddMinutes(10));

        // Act
        method.ClearSetupVerificationCode();

        // Assert
        method.GetSetupVerificationCode().Should().BeNull();
    }

    [Fact]
    public void ClearSetupVerificationCode_ShouldHandleEmptyMetadata()
    {
        // Arrange
        var method = MfaMethod.CreateEmail(_userId, "user@example.com");

        // Act & Assert - Should not throw
        var act = () => method.ClearSetupVerificationCode();
        act.Should().NotThrow();
    }

    #endregion

    #region Metadata Format Tests

    [Fact]
    public void TotpMetadata_ShouldContainCorrectFormat()
    {
        // Act
        var method = MfaMethod.CreateTotp(_userId, "secret", "Test TOTP");

        // Assert
        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(method.Metadata!);
        metadata!.Should().ContainKey("Algorithm");
        metadata.Should().ContainKey("Digits");
        metadata.Should().ContainKey("Period");

        // Check default values
        metadata["Algorithm"].ToString().Should().Be("SHA1");
        metadata["Digits"].ToString().Should().Be("6");
        metadata["Period"].ToString().Should().Be("30");
    }

    [Fact]
    public void WebAuthnMetadata_ShouldContainCorrectFormat()
    {
        // Act
        var method = MfaMethod.CreateWebAuthn(_userId, "cred-123", "public-key", "YubiKey");

        // Assert
        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(method.Metadata!);
        metadata!.Should().ContainKey("PublicKey");
        metadata.Should().ContainKey("Counter");
        metadata.Should().ContainKey("DeviceName");
        metadata.Should().ContainKey("AAGUID");

        metadata["PublicKey"].ToString().Should().Be("public-key");
        metadata["DeviceName"].ToString().Should().Be("YubiKey");
        metadata["Counter"].ToString().Should().Be("0");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void MultipleOperations_ShouldMaintainConsistentState()
    {
        // Arrange
        var method = MfaMethod.CreateTotp(_userId, "secret", "Test Method");

        // Act - Perform multiple operations
        method.Verify();
        method.SetAsDefault();
        method.RecordUsage();
        method.UpdateName("Updated Method");

        // Assert
        method.IsEnabled.Should().BeTrue();
        method.IsDefault.Should().BeTrue();
        method.LastUsedAt.Should().NotBeNull();
        method.Name.Should().Be("Updated Method");
        method.VerifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void DisableAfterDefault_ShouldRemoveBothStates()
    {
        // Arrange
        var method = MfaMethod.CreateWebAuthn(_userId, "cred", "key", "device");
        method.SetAsDefault();

        // Act
        method.Disable();

        // Assert
        method.IsEnabled.Should().BeFalse();
        method.IsDefault.Should().BeFalse();
    }

    #endregion
}
