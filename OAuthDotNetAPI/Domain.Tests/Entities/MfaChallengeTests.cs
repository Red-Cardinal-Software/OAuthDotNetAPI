using Domain.Entities.Security;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Entities;

public class MfaChallengeTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _mfaMethodId = Guid.NewGuid();
    private const string TestIpAddress = "192.168.1.1";
    private const string TestUserAgent = "Mozilla/5.0 Test Browser";

    #region Creation Tests

    [Fact]
    public void Create_ShouldInitializeCorrectly_WithValidData()
    {
        // Act
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp, _mfaMethodId, TestIpAddress, TestUserAgent);

        // Assert
        challenge.Id.Should().NotBe(Guid.Empty);
        challenge.UserId.Should().Be(_userId);
        challenge.Type.Should().Be(MfaType.Totp);
        challenge.MfaMethodId.Should().Be(_mfaMethodId);
        challenge.IpAddress.Should().Be(TestIpAddress);
        challenge.UserAgent.Should().Be(TestUserAgent);
        challenge.IsCompleted.Should().BeFalse();
        challenge.IsInvalid.Should().BeFalse();
        challenge.AttemptCount.Should().Be(0);
        challenge.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        challenge.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(1));
        challenge.CompletedAt.Should().BeNull();
        challenge.LastAttemptAt.Should().BeNull();
        challenge.Metadata.Should().BeNull();

        // Verify token generation
        challenge.ChallengeToken.Should().NotBeNullOrEmpty();
        challenge.ChallengeToken.Should().MatchRegex(@"^[A-Za-z0-9_-]+$"); // URL-safe base64
    }

    [Fact]
    public void Create_ShouldWork_WithMinimalParameters()
    {
        // Act
        var challenge = MfaChallenge.Create(_userId, MfaType.Email);

        // Assert
        challenge.UserId.Should().Be(_userId);
        challenge.Type.Should().Be(MfaType.Email);
        challenge.MfaMethodId.Should().BeNull();
        challenge.IpAddress.Should().BeNull();
        challenge.UserAgent.Should().BeNull();
        challenge.IsValid().Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueChallenges()
    {
        // Act
        var challenge1 = MfaChallenge.Create(_userId, MfaType.Totp);
        var challenge2 = MfaChallenge.Create(_userId, MfaType.Totp);

        // Assert
        challenge1.Id.Should().NotBe(challenge2.Id);
        challenge1.ChallengeToken.Should().NotBe(challenge2.ChallengeToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Create_ShouldThrow_WhenUserIdIsInvalid(string userIdString)
    {
        // Arrange
        var userId = string.IsNullOrEmpty(userIdString) ? Guid.Empty : Guid.Parse(userIdString);

        // Act & Assert
        var act = () => MfaChallenge.Create(userId, MfaType.Totp);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("userId")
            .WithMessage("User ID cannot be empty*");
    }

    #endregion

    #region Token Generation Tests

    [Fact]
    public void ChallengeToken_ShouldBeUrlSafe()
    {
        // Act
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);

        // Assert
        challenge.ChallengeToken.Should().NotContain("+");
        challenge.ChallengeToken.Should().NotContain("/");
        challenge.ChallengeToken.Should().NotContain("=");
        challenge.ChallengeToken.Should().MatchRegex(@"^[A-Za-z0-9_-]+$");
    }

    [Fact]
    public void ChallengeToken_ShouldBeSecureLength()
    {
        // Act
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);

        // Assert - 32 bytes -> 43 characters in base64 (without padding)
        challenge.ChallengeToken.Length.Should().BeGreaterThanOrEqualTo(40);
        challenge.ChallengeToken.Length.Should().BeLessThanOrEqualTo(45);
    }

    [Fact]
    public void ChallengeToken_ShouldBeUnique()
    {
        // Act - Generate multiple tokens
        var tokens = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var challenge = MfaChallenge.Create(Guid.NewGuid(), MfaType.Totp);
            tokens.Add(challenge.ChallengeToken);
        }

        // Assert - All tokens should be unique
        tokens.Should().HaveCount(100, "cryptographically secure tokens should not collide");
    }

    #endregion

    #region Attempt Tracking Tests

    [Fact]
    public void RecordFailedAttempt_ShouldIncrementCount_WhenValid()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);

        // Act
        var canContinue = challenge.RecordFailedAttempt();

        // Assert
        canContinue.Should().BeTrue();
        challenge.AttemptCount.Should().Be(1);
        challenge.LastAttemptAt.Should().NotBeNull();
        challenge.LastAttemptAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        challenge.IsValid().Should().BeTrue();
    }

    [Fact]
    public void RecordFailedAttempt_ShouldInvalidateAfterMaxAttempts()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);

        // Act - Record 3 failed attempts (max)
        challenge.RecordFailedAttempt().Should().BeTrue(); // 1st attempt
        challenge.RecordFailedAttempt().Should().BeTrue(); // 2nd attempt
        var canContinue = challenge.RecordFailedAttempt(); // 3rd attempt

        // Assert
        canContinue.Should().BeFalse();
        challenge.AttemptCount.Should().Be(3);
        challenge.IsInvalid.Should().BeTrue();
        challenge.IsValid().Should().BeFalse();
        challenge.GetRemainingAttempts().Should().Be(0);
    }

    [Fact]
    public void RecordFailedAttempt_ShouldThrow_WhenAlreadyCompleted()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);
        challenge.Complete();

        // Act & Assert
        var act = () => challenge.RecordFailedAttempt();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot record attempt on invalid or completed challenge");
    }

    [Fact]
    public void RecordFailedAttempt_ShouldThrow_WhenInvalid()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);
        challenge.Invalidate();

        // Act & Assert
        var act = () => challenge.RecordFailedAttempt();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot record attempt on invalid or completed challenge");
    }

    #endregion

    #region Completion Tests

    [Fact]
    public void Complete_ShouldMarkAsCompleted_WhenValid()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);

        // Act
        challenge.Complete();

        // Assert
        challenge.IsCompleted.Should().BeTrue();
        challenge.CompletedAt.Should().NotBeNull();
        challenge.CompletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        challenge.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Complete_ShouldThrow_WhenAlreadyCompleted()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);
        challenge.Complete();

        // Act & Assert
        var act = () => challenge.Complete();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Challenge is already completed");
    }

    [Fact]
    public void Complete_ShouldThrow_WhenInvalid()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);
        challenge.Invalidate();

        // Act & Assert
        var act = () => challenge.Complete();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot complete an invalid challenge");
    }

    #endregion

    #region Invalidation Tests

    [Fact]
    public void Invalidate_ShouldMarkAsInvalid()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);

        // Act
        challenge.Invalidate();

        // Assert
        challenge.IsInvalid.Should().BeTrue();
        challenge.IsValid().Should().BeFalse();
        challenge.GetRemainingAttempts().Should().Be(0);
    }

    [Fact]
    public void Invalidate_ShouldWork_WhenAlreadyInvalid()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);
        challenge.Invalidate();

        // Act & Assert - Should not throw
        var act = () => challenge.Invalidate();
        act.Should().NotThrow();
        challenge.IsInvalid.Should().BeTrue();
    }

    #endregion

    #region Validity and Expiration Tests

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenNotExpired()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);

        // Act & Assert
        challenge.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenNewChallenge()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);

        // Act & Assert
        challenge.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenCompleted()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);
        challenge.Complete();

        // Act & Assert
        challenge.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenInvalid()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);
        challenge.Invalidate();

        // Act & Assert
        challenge.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenMaxAttemptsReached()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);
        challenge.RecordFailedAttempt();
        challenge.RecordFailedAttempt();
        challenge.RecordFailedAttempt();

        // Act & Assert
        challenge.IsValid().Should().BeFalse();
    }

    #endregion

    #region Remaining Attempts Tests

    [Fact]
    public void GetRemainingAttempts_ShouldReturnThree_WhenNew()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);

        // Act & Assert
        challenge.GetRemainingAttempts().Should().Be(3);
    }

    [Fact]
    public void GetRemainingAttempts_ShouldDecrement_AfterFailedAttempts()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);

        // Act & Assert
        challenge.GetRemainingAttempts().Should().Be(3);
        
        challenge.RecordFailedAttempt();
        challenge.GetRemainingAttempts().Should().Be(2);
        
        challenge.RecordFailedAttempt();
        challenge.GetRemainingAttempts().Should().Be(1);
        
        challenge.RecordFailedAttempt();
        challenge.GetRemainingAttempts().Should().Be(0);
    }

    [Fact]
    public void GetRemainingAttempts_ShouldReturnZero_WhenCompleted()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);
        challenge.Complete();

        // Act & Assert
        challenge.GetRemainingAttempts().Should().Be(0);
    }

    [Fact]
    public void GetRemainingAttempts_ShouldReturnZero_WhenInvalid()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);
        challenge.Invalidate();

        // Act & Assert
        challenge.GetRemainingAttempts().Should().Be(0);
    }

    #endregion

    #region Remaining Time Tests

    [Fact]
    public void GetRemainingTime_ShouldReturnPositive_WhenNotExpired()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);

        // Act
        var remaining = challenge.GetRemainingTime();

        // Assert
        remaining.Should().BeGreaterThan(TimeSpan.Zero);
        remaining.Should().BeLessThanOrEqualTo(TimeSpan.FromMinutes(5));
        remaining.Should().BeCloseTo(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetRemainingTime_ShouldReturnZero_WhenExpired()
    {
        // Arrange - Create challenge with past expiration
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp);
        
        // Use reflection to set ExpiresAt to past time (simulating expired challenge)
        var expiresAtProperty = typeof(MfaChallenge).GetProperty(nameof(MfaChallenge.ExpiresAt))!;
        expiresAtProperty.SetValue(challenge, DateTimeOffset.UtcNow.AddMinutes(-1));

        // Act
        var remaining = challenge.GetRemainingTime();

        // Assert
        remaining.Should().Be(TimeSpan.Zero);
        challenge.IsExpired().Should().BeTrue();
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public void SetMetadata_ShouldUpdateMetadata_WhenValid()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Email);
        const string metadata = """{"email": "user@example.com", "masked": "u***@example.com"}""";

        // Act
        challenge.SetMetadata(metadata);

        // Assert
        challenge.Metadata.Should().Be(metadata);
    }

    [Fact]
    public void SetMetadata_ShouldThrow_WhenCompleted()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Email);
        challenge.Complete();

        // Act & Assert
        var act = () => challenge.SetMetadata("test");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update metadata on completed or invalid challenge");
    }

    [Fact]
    public void SetMetadata_ShouldThrow_WhenInvalid()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Email);
        challenge.Invalidate();

        // Act & Assert
        var act = () => challenge.SetMetadata("test");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update metadata on completed or invalid challenge");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_ShouldWork_WhenSuccessful()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Totp, _mfaMethodId, TestIpAddress, TestUserAgent);

        // Act & Assert - Initial state
        challenge.IsValid().Should().BeTrue();
        challenge.GetRemainingAttempts().Should().Be(3);

        // Act & Assert - Set metadata
        challenge.SetMetadata("""{"deviceId": "device123"}""");
        challenge.Metadata.Should().Contain("device123");

        // Act & Assert - Record some failed attempts
        challenge.RecordFailedAttempt().Should().BeTrue();
        challenge.GetRemainingAttempts().Should().Be(2);
        challenge.IsValid().Should().BeTrue();

        challenge.RecordFailedAttempt().Should().BeTrue();
        challenge.GetRemainingAttempts().Should().Be(1);
        challenge.IsValid().Should().BeTrue();

        // Act & Assert - Complete successfully
        challenge.Complete();
        challenge.IsCompleted.Should().BeTrue();
        challenge.IsValid().Should().BeFalse();
        challenge.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void FullWorkflow_ShouldInvalidate_WhenTooManyFailures()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Push);

        // Act & Assert - Record maximum failed attempts
        challenge.RecordFailedAttempt().Should().BeTrue();  // 1st
        challenge.RecordFailedAttempt().Should().BeTrue();  // 2nd
        challenge.RecordFailedAttempt().Should().BeFalse(); // 3rd - should invalidate

        // Assert final state
        challenge.IsInvalid.Should().BeTrue();
        challenge.IsCompleted.Should().BeFalse();
        challenge.IsValid().Should().BeFalse();
        challenge.AttemptCount.Should().Be(3);
    }

    #endregion

    #region MFA Type Tests

    [Theory]
    [InlineData(MfaType.Totp)]
    [InlineData(MfaType.WebAuthn)]
    [InlineData(MfaType.Email)]
    [InlineData(MfaType.Push)]
    public void Create_ShouldWork_WithAllMfaTypes(MfaType mfaType)
    {
        // Act
        var challenge = MfaChallenge.Create(_userId, mfaType);

        // Assert
        challenge.Type.Should().Be(mfaType);
        challenge.IsValid().Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Multiple_Operations_ShouldMaintainConsistentState()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.WebAuthn, _mfaMethodId, TestIpAddress, TestUserAgent);

        // Act - Perform multiple operations
        challenge.SetMetadata("""{"credentialId": "cred123"}""");
        challenge.RecordFailedAttempt();

        // Assert state consistency
        challenge.AttemptCount.Should().Be(1);
        challenge.IsValid().Should().BeTrue();
        challenge.Metadata.Should().Contain("cred123");
        challenge.LastAttemptAt.Should().NotBeNull();
        challenge.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void InvalidateAfterAttempts_ShouldPreventFurtherOperations()
    {
        // Arrange
        var challenge = MfaChallenge.Create(_userId, MfaType.Email);
        challenge.RecordFailedAttempt();
        challenge.RecordFailedAttempt();
        challenge.RecordFailedAttempt(); // This should invalidate

        // Act & Assert
        challenge.IsInvalid.Should().BeTrue();
        
        var act1 = () => challenge.SetMetadata("test");
        act1.Should().Throw<InvalidOperationException>();
        
        var act2 = () => challenge.Complete();
        act2.Should().Throw<InvalidOperationException>();
    }

    #endregion
}
