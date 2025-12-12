using Domain.Entities.Security;
using Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Entities;

public class MfaPushChallengeTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _deviceId = Guid.NewGuid();
    private const string TestSessionId = "session-123";
    private const string TestIpAddress = "192.168.1.1";
    private const string TestUserAgent = "Mozilla/5.0 Test Browser";
    private const string TestLocation = "New York, NY, USA";
    private const string TestSignature = "test-signature-12345";

    #region Creation Tests

    [Fact]
    public void Constructor_ShouldInitializeCorrectly_WithValidData()
    {
        // Act
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Assert
        challenge.Id.Should().NotBe(Guid.Empty);
        challenge.UserId.Should().Be(_userId);
        challenge.DeviceId.Should().Be(_deviceId);
        challenge.SessionId.Should().Be(TestSessionId);
        challenge.IpAddress.Should().Be(TestIpAddress);
        challenge.UserAgent.Should().Be(TestUserAgent);
        challenge.Location.Should().BeNull();
        challenge.ContextData.Should().BeNull();
        challenge.Status.Should().Be(ChallengeStatus.Pending);
        challenge.Response.Should().Be(ChallengeResponse.None);
        challenge.RespondedAt.Should().BeNull();
        challenge.ResponseSignature.Should().BeNull();
        challenge.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        challenge.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(1));
        
        // Verify challenge code generation
        challenge.ChallengeCode.Should().NotBeNullOrEmpty();
        challenge.ChallengeCode.Should().HaveLength(16);
        challenge.ChallengeCode.Should().MatchRegex(@"^[A-Za-z0-9]+$");
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly_WithCustomExpiryMinutes()
    {
        // Arrange
        const int customExpiry = 10;

        // Act
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent, customExpiry);

        // Assert
        challenge.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(customExpiry), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueIds()
    {
        // Act
        var challenge1 = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);
        var challenge2 = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Assert
        challenge1.Id.Should().NotBe(challenge2.Id);
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueChallengeCode()
    {
        // Act
        var challenge1 = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);
        var challenge2 = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Assert
        challenge1.ChallengeCode.Should().NotBe(challenge2.ChallengeCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenSessionIdIsInvalid(string? sessionId)
    {
        // Act & Assert
        var act = () => new MfaPushChallenge(_userId, _deviceId, sessionId!, TestIpAddress, TestUserAgent);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("sessionId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenIpAddressIsInvalid(string? ipAddress)
    {
        // Act & Assert
        var act = () => new MfaPushChallenge(_userId, _deviceId, TestSessionId, ipAddress!, TestUserAgent);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("ipAddress");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenUserAgentIsInvalid(string? userAgent)
    {
        // Act & Assert
        var act = () => new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, userAgent!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("userAgent");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(31)]
    [InlineData(60)]
    public void Constructor_ShouldThrow_WhenExpiryMinutesIsInvalid(int expiryMinutes)
    {
        // Act & Assert
        var act = () => new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent, expiryMinutes);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("expiryMinutes")
            .WithMessage("Expiry must be between 1 and 30 minutes*");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(30)]
    public void Constructor_ShouldAccept_ValidExpiryMinutes(int expiryMinutes)
    {
        // Act & Assert
        var act = () => new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent, expiryMinutes);
        act.Should().NotThrow();
        
        var challenge = act();
        challenge.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(expiryMinutes), TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Challenge Code Generation Tests

    [Fact]
    public void ChallengeCode_ShouldBeUrlSafe()
    {
        // Act
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Assert
        challenge.ChallengeCode.Should().NotContain("+");
        challenge.ChallengeCode.Should().NotContain("/");
        challenge.ChallengeCode.Should().NotContain("=");
    }

    [Fact]
    public void ChallengeCode_ShouldHaveCorrectLength()
    {
        // Act
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Assert
        challenge.ChallengeCode.Should().HaveLength(16);
    }

    [Fact]
    public void ChallengeCode_ShouldBeUnique()
    {
        // Act - Generate multiple challenge codes
        var codes = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var challenge = new MfaPushChallenge(Guid.NewGuid(), Guid.NewGuid(), $"session-{i}", TestIpAddress, TestUserAgent);
            codes.Add(challenge.ChallengeCode);
        }

        // Assert - All codes should be unique
        codes.Should().HaveCount(100, "challenge codes should be cryptographically unique");
    }

    #endregion

    #region Metadata Methods Tests

    [Fact]
    public void SetLocation_ShouldUpdateLocation_WhenValidString()
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Act
        challenge.SetLocation(TestLocation);

        // Assert
        challenge.Location.Should().Be(TestLocation);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetLocation_ShouldNotUpdateLocation_WhenStringIsInvalid(string? location)
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Act
        challenge.SetLocation(location!);

        // Assert
        challenge.Location.Should().BeNull();
    }

    [Fact]
    public void SetContextData_ShouldUpdateContextData_WhenValidString()
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);
        const string contextData = """{"deviceModel": "iPhone 14", "appVersion": "2.1.0"}""";

        // Act
        challenge.SetContextData(contextData);

        // Assert
        challenge.ContextData.Should().Be(contextData);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetContextData_ShouldNotUpdateContextData_WhenStringIsInvalid(string? contextData)
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Act
        challenge.SetContextData(contextData!);

        // Assert
        challenge.ContextData.Should().BeNull();
    }

    #endregion

    #region Approval Tests

    [Fact]
    public void Approve_ShouldSetCorrectState_WhenValid()
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Act
        challenge.Approve(TestSignature);

        // Assert
        challenge.Status.Should().Be(ChallengeStatus.Approved);
        challenge.Response.Should().Be(ChallengeResponse.Approved);
        challenge.ResponseSignature.Should().Be(TestSignature);
        challenge.RespondedAt.Should().NotBeNull();
        challenge.RespondedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Approve_ShouldThrow_WhenSignatureIsInvalid(string? signature)
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Act & Assert
        var act = () => challenge.Approve(signature!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("signature");
    }

    [Theory]
    [InlineData(ChallengeStatus.Approved)]
    [InlineData(ChallengeStatus.Denied)]
    [InlineData(ChallengeStatus.Expired)]
    [InlineData(ChallengeStatus.Consumed)]
    public void Approve_ShouldThrow_WhenStatusIsNotPending(ChallengeStatus status)
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);
        SetChallengeStatus(challenge, status);

        // Act & Assert
        var act = () => challenge.Approve(TestSignature);
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage($"Challenge is already {status}");
    }

    [Fact]
    public void Approve_ShouldThrow_WhenChallengeIsExpired()
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);
        SetChallengeExpired(challenge);

        // Act & Assert
        var act = () => challenge.Approve(TestSignature);
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("Challenge has expired");
    }

    #endregion

    #region Denial Tests

    [Fact]
    public void Deny_ShouldSetCorrectState_WhenValid()
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Act
        challenge.Deny(TestSignature);

        // Assert
        challenge.Status.Should().Be(ChallengeStatus.Denied);
        challenge.Response.Should().Be(ChallengeResponse.Denied);
        challenge.ResponseSignature.Should().Be(TestSignature);
        challenge.RespondedAt.Should().NotBeNull();
        challenge.RespondedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deny_ShouldThrow_WhenSignatureIsInvalid(string? signature)
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Act & Assert
        var act = () => challenge.Deny(signature!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("signature");
    }

    [Theory]
    [InlineData(ChallengeStatus.Approved)]
    [InlineData(ChallengeStatus.Denied)]
    [InlineData(ChallengeStatus.Expired)]
    [InlineData(ChallengeStatus.Consumed)]
    public void Deny_ShouldThrow_WhenStatusIsNotPending(ChallengeStatus status)
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);
        SetChallengeStatus(challenge, status);

        // Act & Assert
        var act = () => challenge.Deny(TestSignature);
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage($"Challenge is already {status}");
    }

    [Fact]
    public void Deny_ShouldThrow_WhenChallengeIsExpired()
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);
        SetChallengeExpired(challenge);

        // Act & Assert
        var act = () => challenge.Deny(TestSignature);
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("Challenge has expired");
    }

    #endregion

    #region Expiration Tests

    [Fact]
    public void MarkExpired_ShouldSetStatus_WhenPending()
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Act
        challenge.MarkExpired();

        // Assert
        challenge.Status.Should().Be(ChallengeStatus.Expired);
    }

    [Theory]
    [InlineData(ChallengeStatus.Approved)]
    [InlineData(ChallengeStatus.Denied)]
    [InlineData(ChallengeStatus.Expired)]
    [InlineData(ChallengeStatus.Consumed)]
    public void MarkExpired_ShouldThrow_WhenStatusIsNotPending(ChallengeStatus status)
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);
        SetChallengeStatus(challenge, status);

        // Act & Assert
        var act = () => challenge.MarkExpired();
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("Only pending challenges can be expired");
    }

    #endregion

    #region Consumption Tests

    [Fact]
    public void MarkConsumed_ShouldSetStatus_WhenApproved()
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);
        challenge.Approve(TestSignature);

        // Act
        challenge.MarkConsumed();

        // Assert
        challenge.Status.Should().Be(ChallengeStatus.Consumed);
    }

    [Theory]
    [InlineData(ChallengeStatus.Pending)]
    [InlineData(ChallengeStatus.Denied)]
    [InlineData(ChallengeStatus.Expired)]
    [InlineData(ChallengeStatus.Consumed)]
    public void MarkConsumed_ShouldThrow_WhenStatusIsNotApproved(ChallengeStatus status)
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);
        SetChallengeStatus(challenge, status);

        // Act & Assert
        var act = () => challenge.MarkConsumed();
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("Only approved challenges can be consumed");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_Approval_ShouldWork()
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Act & Assert - Set metadata
        challenge.SetLocation(TestLocation);
        challenge.SetContextData("""{"deviceModel": "iPhone 14"}""");
        challenge.Location.Should().Be(TestLocation);
        challenge.ContextData.Should().Contain("iPhone 14");

        // Act & Assert - Approve challenge
        challenge.Approve(TestSignature);
        challenge.Status.Should().Be(ChallengeStatus.Approved);
        challenge.Response.Should().Be(ChallengeResponse.Approved);

        // Act & Assert - Mark as consumed
        challenge.MarkConsumed();
        challenge.Status.Should().Be(ChallengeStatus.Consumed);
    }

    [Fact]
    public void FullWorkflow_Denial_ShouldWork()
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Act & Assert - Set metadata
        challenge.SetLocation(TestLocation);
        challenge.Location.Should().Be(TestLocation);

        // Act & Assert - Deny challenge
        challenge.Deny(TestSignature);
        challenge.Status.Should().Be(ChallengeStatus.Denied);
        challenge.Response.Should().Be(ChallengeResponse.Denied);

        // Verify cannot be consumed after denial
        var act = () => challenge.MarkConsumed();
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void FullWorkflow_Expiration_ShouldWork()
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Act & Assert - Mark as expired
        challenge.MarkExpired();
        challenge.Status.Should().Be(ChallengeStatus.Expired);

        // Verify cannot be approved after expiration
        var actApprove = () => challenge.Approve(TestSignature);
        actApprove.Should().Throw<InvalidStateTransitionException>();

        // Verify cannot be denied after expiration
        var actDeny = () => challenge.Deny(TestSignature);
        actDeny.Should().Throw<InvalidStateTransitionException>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void MultipleMetadataUpdates_ShouldOverwrite()
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Act
        challenge.SetLocation("First Location");
        challenge.SetLocation("Second Location");
        
        challenge.SetContextData("""{"first": "data"}""");
        challenge.SetContextData("""{"second": "data"}""");

        // Assert
        challenge.Location.Should().Be("Second Location");
        challenge.ContextData.Should().Be("""{"second": "data"}""");
    }

    [Fact]
    public void StateTransitions_ShouldBeIrreversible()
    {
        // Arrange
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Act - Approve, then try other operations
        challenge.Approve(TestSignature);
        
        // Assert - Cannot deny after approval
        var actDeny = () => challenge.Deny("other-signature");
        actDeny.Should().Throw<InvalidStateTransitionException>();

        // Assert - Cannot expire after approval
        var actExpire = () => challenge.MarkExpired();
        actExpire.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void DefaultExpiryTime_ShouldBeFiveMinutes()
    {
        // Act
        var challenge = new MfaPushChallenge(_userId, _deviceId, TestSessionId, TestIpAddress, TestUserAgent);

        // Assert
        var expectedExpiry = challenge.CreatedAt.AddMinutes(5);
        challenge.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Helper Methods

    private static void SetChallengeStatus(MfaPushChallenge challenge, ChallengeStatus status)
    {
        var statusProperty = typeof(MfaPushChallenge).GetProperty(nameof(MfaPushChallenge.Status))!;
        statusProperty.SetValue(challenge, status);
    }

    private static void SetChallengeExpired(MfaPushChallenge challenge)
    {
        var expiresAtProperty = typeof(MfaPushChallenge).GetProperty(nameof(MfaPushChallenge.ExpiresAt))!;
        expiresAtProperty.SetValue(challenge, DateTime.UtcNow.AddMinutes(-1));
    }

    #endregion
}
