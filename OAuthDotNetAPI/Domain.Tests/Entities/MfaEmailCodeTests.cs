using Domain.Entities.Security;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Entities;

public class MfaEmailCodeTests
{
    private readonly Guid _challengeId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private const string TestEmail = "test@example.com";
    private const string TestHashedCode = "hashed-code-123";
    private const string TestIpAddress = "192.168.1.1";

    [Fact]
    public void Create_ShouldInitializeCorrectly_WithValidData()
    {
        // Act
        var (emailCode, plainCode) = MfaEmailCode.Create(_challengeId, _userId, TestEmail, TestHashedCode, TestIpAddress);

        // Assert
        emailCode.Id.Should().NotBe(Guid.Empty);
        emailCode.MfaChallengeId.Should().Be(_challengeId);
        emailCode.UserId.Should().Be(_userId);
        emailCode.EmailAddress.Should().Be(TestEmail.ToLowerInvariant());
        emailCode.HashedCode.Should().Be(TestHashedCode);
        emailCode.IsUsed.Should().BeFalse();
        emailCode.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(1));
        emailCode.SentAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        emailCode.UsedAt.Should().BeNull();
        emailCode.AttemptCount.Should().Be(0);
        emailCode.IpAddress.Should().Be(TestIpAddress);

        // Verify generated code format
        plainCode.Should().NotBeNullOrEmpty();
        plainCode.Should().HaveLength(8);
        plainCode.Should().MatchRegex("^[0-9]{8}$");
        int.Parse(plainCode).Should().BeInRange(10000000, 99999999);
    }

    [Fact]
    public void Create_ShouldNormalizeEmail_ToLowerCase()
    {
        // Arrange
        const string upperCaseEmail = "TEST@EXAMPLE.COM";

        // Act
        var (emailCode, _) = MfaEmailCode.Create(_challengeId, _userId, upperCaseEmail, TestHashedCode);

        // Assert
        emailCode.EmailAddress.Should().Be("test@example.com");
    }

    [Fact]
    public void Create_ShouldWork_WithoutIpAddress()
    {
        // Act
        var (emailCode, _) = MfaEmailCode.Create(_challengeId, _userId, TestEmail, TestHashedCode);

        // Assert
        emailCode.IpAddress.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueCodes()
    {
        // Act
        var (code1, plainCode1) = MfaEmailCode.Create(_challengeId, _userId, TestEmail, TestHashedCode);
        var (code2, plainCode2) = MfaEmailCode.Create(_challengeId, _userId, TestEmail, TestHashedCode);

        // Assert
        code1.Id.Should().NotBe(code2.Id);
        plainCode1.Should().NotBe(plainCode2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Create_ShouldThrow_WhenChallengeIdIsInvalid(string challengeIdString)
    {
        // Arrange
        var challengeId = string.IsNullOrEmpty(challengeIdString) ? Guid.Empty : Guid.Parse(challengeIdString);

        // Act & Assert
        var act = () => MfaEmailCode.Create(challengeId, _userId, TestEmail, TestHashedCode);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("challengeId")
            .WithMessage("Challenge ID cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Create_ShouldThrow_WhenUserIdIsInvalid(string userIdString)
    {
        // Arrange
        var userId = string.IsNullOrEmpty(userIdString) ? Guid.Empty : Guid.Parse(userIdString);

        // Act & Assert
        var act = () => MfaEmailCode.Create(_challengeId, userId, TestEmail, TestHashedCode);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("userId")
            .WithMessage("User ID cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenEmailAddressIsInvalid(string? email)
    {
        // Act & Assert
        var act = () => MfaEmailCode.Create(_challengeId, _userId, email!, TestHashedCode);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("emailAddress")
            .WithMessage("Email address cannot be empty*");
    }

    [Fact]
    public void RecordAttempt_ShouldIncrementAttemptCount_WhenValid()
    {
        // Arrange
        var (emailCode, _) = MfaEmailCode.Create(_challengeId, _userId, TestEmail, TestHashedCode);

        // Act
        var result = emailCode.RecordAttempt();

        // Assert
        result.Should().BeTrue();
        emailCode.AttemptCount.Should().Be(1);
    }

    [Fact]
    public void RecordAttempt_ShouldReturnFalse_WhenMaxAttemptsReached()
    {
        // Arrange
        var (emailCode, _) = MfaEmailCode.Create(_challengeId, _userId, TestEmail, TestHashedCode);

        // Max out attempts (3)
        emailCode.RecordAttempt();
        emailCode.RecordAttempt();
        emailCode.RecordAttempt();

        // Act
        var result = emailCode.RecordAttempt();

        // Assert
        result.Should().BeFalse();
        emailCode.AttemptCount.Should().Be(3);
    }

    [Fact]
    public void RecordAttempt_ShouldReturnFalse_WhenAlreadyUsed()
    {
        // Arrange
        var (emailCode, _) = MfaEmailCode.Create(_challengeId, _userId, TestEmail, TestHashedCode);
        emailCode.MarkAsUsed();

        // Act
        var result = emailCode.RecordAttempt();

        // Assert
        result.Should().BeFalse();
        emailCode.AttemptCount.Should().Be(0);
    }

    [Fact]
    public void MarkAsUsed_ShouldSetUsedProperties()
    {
        // Arrange
        var (emailCode, _) = MfaEmailCode.Create(_challengeId, _userId, TestEmail, TestHashedCode);

        // Act
        emailCode.MarkAsUsed();

        // Assert
        emailCode.IsUsed.Should().BeTrue();
        emailCode.UsedAt.Should().NotBeNull();
        emailCode.UsedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenNotUsedNotExpiredAndAttemptsRemaining()
    {
        // Arrange
        var (emailCode, _) = MfaEmailCode.Create(_challengeId, _userId, TestEmail, TestHashedCode);

        // Act & Assert
        emailCode.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenUsed()
    {
        // Arrange
        var (emailCode, _) = MfaEmailCode.Create(_challengeId, _userId, TestEmail, TestHashedCode);
        emailCode.MarkAsUsed();

        // Act & Assert
        emailCode.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenMaxAttemptsReached()
    {
        // Arrange
        var (emailCode, _) = MfaEmailCode.Create(_challengeId, _userId, TestEmail, TestHashedCode);
        emailCode.RecordAttempt();
        emailCode.RecordAttempt();
        emailCode.RecordAttempt();

        // Act & Assert
        emailCode.IsValid().Should().BeFalse();
    }

    [Fact]
    public void GetRemainingAttempts_ShouldReturnCorrectCount()
    {
        // Arrange
        var (emailCode, _) = MfaEmailCode.Create(_challengeId, _userId, TestEmail, TestHashedCode);

        // Act & Assert - Initial state
        emailCode.GetRemainingAttempts().Should().Be(3);

        // Act & Assert - After one attempt
        emailCode.RecordAttempt();
        emailCode.GetRemainingAttempts().Should().Be(2);

        // Act & Assert - After two attempts
        emailCode.RecordAttempt();
        emailCode.GetRemainingAttempts().Should().Be(1);

        // Act & Assert - After max attempts
        emailCode.RecordAttempt();
        emailCode.GetRemainingAttempts().Should().Be(0);
    }

    [Fact]
    public void GetRemainingAttempts_ShouldNotReturnNegative()
    {
        // Arrange
        var (emailCode, _) = MfaEmailCode.Create(_challengeId, _userId, TestEmail, TestHashedCode);

        // Force attempt count beyond maximum (simulate edge case)
        emailCode.RecordAttempt();
        emailCode.RecordAttempt();
        emailCode.RecordAttempt();
        emailCode.RecordAttempt(); // This should fail but let's test the remaining calculation

        // Act & Assert
        emailCode.GetRemainingAttempts().Should().Be(0);
    }

    [Fact]
    public void Invalidate_ShouldMarkAsUsed()
    {
        // Arrange
        var (emailCode, _) = MfaEmailCode.Create(_challengeId, _userId, TestEmail, TestHashedCode);

        // Act
        emailCode.Invalidate();

        // Assert
        emailCode.IsUsed.Should().BeTrue();
        emailCode.UsedAt.Should().NotBeNull();
        emailCode.UsedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        emailCode.IsValid().Should().BeFalse();
    }

    [Fact]
    public void CodeGeneration_ShouldProduceCryptographicallySecureCodes()
    {
        // Arrange - Generate multiple codes to check for patterns
        var codes = new HashSet<string>();

        // Act - Generate 100 codes
        for (int i = 0; i < 100; i++)
        {
            var (_, plainCode) = MfaEmailCode.Create(Guid.NewGuid(), Guid.NewGuid(), TestEmail, TestHashedCode);
            codes.Add(plainCode);
        }

        // Assert - All codes should be unique (extremely high probability with crypto RNG)
        codes.Should().HaveCount(100, "cryptographically secure random codes should not collide");

        // Assert - All codes should be 8 digits
        foreach (var code in codes)
        {
            code.Should().HaveLength(8);
            code.Should().MatchRegex("^[0-9]{8}$");
            int.Parse(code).Should().BeInRange(10000000, 99999999);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RecordAttempt_ShouldAllowAttemptsUpToLimit(int priorAttempts)
    {
        // Arrange
        var (emailCode, _) = MfaEmailCode.Create(_challengeId, _userId, TestEmail, TestHashedCode);

        // Record prior attempts
        for (int i = 0; i < priorAttempts; i++)
        {
            emailCode.RecordAttempt();
        }

        // Act
        var result = emailCode.RecordAttempt();

        // Assert
        if (priorAttempts < 3)
        {
            result.Should().BeTrue();
            emailCode.AttemptCount.Should().Be(priorAttempts + 1);
        }
        else
        {
            result.Should().BeFalse();
            emailCode.AttemptCount.Should().Be(3);
        }
    }
}
