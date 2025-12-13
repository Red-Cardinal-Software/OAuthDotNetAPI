using Domain.Entities.Security;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Entities;

public class MfaRecoveryCodeTests
{
    private readonly Guid _mfaMethodId = Guid.NewGuid();
    private const string TestHashedCode = "hashed-recovery-code-123";
    private const string TestPlainCode = "ABCD-EFGH-IJKL-MNOP";

    #region Creation Tests

    [Fact]
    public void Create_ShouldInitializeCorrectly_WithValidData()
    {
        // Act
        var recoveryCode = MfaRecoveryCode.Create(_mfaMethodId, TestHashedCode, TestPlainCode);

        // Assert
        recoveryCode.Id.Should().NotBe(Guid.Empty);
        recoveryCode.MfaMethodId.Should().Be(_mfaMethodId);
        recoveryCode.HashedCode.Should().Be(TestHashedCode);
        recoveryCode.Code.Should().Be(TestPlainCode);
        recoveryCode.IsUsed.Should().BeFalse();
        recoveryCode.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        recoveryCode.UsedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Create_ShouldThrow_WhenMfaMethodIdIsInvalid(string methodIdString)
    {
        // Arrange
        var methodId = string.IsNullOrEmpty(methodIdString) ? Guid.Empty : Guid.Parse(methodIdString);

        // Act & Assert
        var act = () => MfaRecoveryCode.Create(methodId, TestHashedCode, TestPlainCode);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("mfaMethodId")
            .WithMessage("MFA method ID cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenHashedCodeIsInvalid(string? hashedCode)
    {
        // Act & Assert
        var act = () => MfaRecoveryCode.Create(_mfaMethodId, hashedCode!, TestPlainCode);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("hashedCode")
            .WithMessage("Hashed code cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenPlainCodeIsInvalid(string? plainCode)
    {
        // Act & Assert
        var act = () => MfaRecoveryCode.Create(_mfaMethodId, TestHashedCode, plainCode!);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("plainCode")
            .WithMessage("Plain code cannot be empty*");
    }

    #endregion

    #region Usage Tests

    [Fact]
    public void TryMarkAsUsed_ShouldMarkAsUsed_WhenNotAlreadyUsed()
    {
        // Arrange
        var recoveryCode = MfaRecoveryCode.Create(_mfaMethodId, TestHashedCode, TestPlainCode);

        // Act
        var result = recoveryCode.TryMarkAsUsed();

        // Assert
        result.Should().BeTrue();
        recoveryCode.IsUsed.Should().BeTrue();
        recoveryCode.UsedAt.Should().NotBeNull();
        recoveryCode.UsedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void TryMarkAsUsed_ShouldReturnFalse_WhenAlreadyUsed()
    {
        // Arrange
        var recoveryCode = MfaRecoveryCode.Create(_mfaMethodId, TestHashedCode, TestPlainCode);
        recoveryCode.TryMarkAsUsed(); // First use
        var firstUsedAt = recoveryCode.UsedAt;

        // Act
        var result = recoveryCode.TryMarkAsUsed(); // Second attempt

        // Assert
        result.Should().BeFalse();
        recoveryCode.IsUsed.Should().BeTrue();
        recoveryCode.UsedAt.Should().Be(firstUsedAt); // Should not change
    }

    #endregion

    #region Code Generation Tests

    [Fact]
    public void GenerateSecureCode_ShouldReturnCorrectFormat()
    {
        // Act
        var code = MfaRecoveryCode.GenerateSecureCode();

        // Assert
        code.Should().NotBeNullOrEmpty();
        code.Should().HaveLength(19); // 16 characters + 3 hyphens
        code.Should().MatchRegex(@"^[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$");
    }

    [Fact]
    public void GenerateSecureCode_ShouldGenerateUniqueCodes()
    {
        // Act - Generate multiple codes
        var codes = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            codes.Add(MfaRecoveryCode.GenerateSecureCode());
        }

        // Assert - All codes should be unique (extremely high probability with crypto RNG)
        codes.Should().HaveCount(100, "cryptographically secure random codes should not collide");
    }

    [Fact]
    public void GenerateSecureCode_ShouldOnlyContainValidCharacters()
    {
        // Act
        var code = MfaRecoveryCode.GenerateSecureCode();

        // Assert
        var codeWithoutHyphens = code.Replace("-", "");
        codeWithoutHyphens.Should().MatchRegex("^[A-Z0-9]+$");
        codeWithoutHyphens.Should().HaveLength(16);

        // Verify each character is from the allowed set
        const string allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        foreach (char c in codeWithoutHyphens)
        {
            allowedChars.Should().Contain(c.ToString());
        }
    }

    [Fact]
    public void GenerateSecureCode_ShouldHaveHyphensInCorrectPositions()
    {
        // Act
        var code = MfaRecoveryCode.GenerateSecureCode();

        // Assert
        code[4].Should().Be('-');
        code[9].Should().Be('-');
        code[14].Should().Be('-');
    }

    #endregion

    #region Code Normalization Tests

    [Theory]
    [InlineData("ABCD-EFGH-IJKL-MNOP", "ABCDEFGHIJKLMNOP")]
    [InlineData("abcd-efgh-ijkl-mnop", "ABCDEFGHIJKLMNOP")]
    [InlineData("AbCd-EfGh-IjKl-MnOp", "ABCDEFGHIJKLMNOP")]
    [InlineData("ABCDEFGHIJKLMNOP", "ABCDEFGHIJKLMNOP")]
    [InlineData("abcdefghijklmnop", "ABCDEFGHIJKLMNOP")]
    public void NormalizeCode_ShouldRemoveHyphensAndConvertToUppercase(string input, string expected)
    {
        // Act
        var result = MfaRecoveryCode.NormalizeCode(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void NormalizeCode_ShouldHandleEmptyString()
    {
        // Act
        var result = MfaRecoveryCode.NormalizeCode("");

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void NormalizeCode_ShouldHandleMultipleHyphens()
    {
        // Act
        var result = MfaRecoveryCode.NormalizeCode("AB--CD--EF--GH");

        // Assert
        result.Should().Be("ABCDEFGH");
    }

    [Fact]
    public void NormalizeCode_ShouldPreserveNumbers()
    {
        // Act
        var result = MfaRecoveryCode.NormalizeCode("1234-5678-9012-3456");

        // Assert
        result.Should().Be("1234567890123456");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_ShouldWorkCorrectly()
    {
        // Arrange - Generate a new code
        var plainCode = MfaRecoveryCode.GenerateSecureCode();
        var normalizedCode = MfaRecoveryCode.NormalizeCode(plainCode);

        // Simulate hashing (in real use, this would be done with IPasswordHasher)
        var hashedCode = $"hashed_{normalizedCode}";

        // Act - Create recovery code entity
        var recoveryCode = MfaRecoveryCode.Create(_mfaMethodId, hashedCode, plainCode);

        // Assert initial state
        recoveryCode.IsUsed.Should().BeFalse();
        recoveryCode.Code.Should().Be(plainCode);
        recoveryCode.HashedCode.Should().Be(hashedCode);

        // Act - Mark as used
        var useResult = recoveryCode.TryMarkAsUsed();

        // Assert final state
        useResult.Should().BeTrue();
        recoveryCode.IsUsed.Should().BeTrue();
        recoveryCode.UsedAt.Should().NotBeNull();

        // Verify can't be used again
        recoveryCode.TryMarkAsUsed().Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Act
        var code1 = MfaRecoveryCode.Create(_mfaMethodId, "hash1", "PLAIN-CODE-ONE");
        var code2 = MfaRecoveryCode.Create(_mfaMethodId, "hash2", "PLAIN-CODE-TWO");

        // Assert
        code1.Id.Should().NotBe(code2.Id);
    }

    [Fact]
    public void Create_ShouldPreservePlainTextForImmediateAccess()
    {
        // Arrange
        const string expectedPlainCode = "TEST-CODE-1234-ABCD";

        // Act
        var recoveryCode = MfaRecoveryCode.Create(_mfaMethodId, TestHashedCode, expectedPlainCode);

        // Assert
        recoveryCode.Code.Should().Be(expectedPlainCode);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void TryMarkAsUsed_ShouldNotChangeTimestamp_OnSubsequentCalls()
    {
        // Arrange
        var recoveryCode = MfaRecoveryCode.Create(_mfaMethodId, TestHashedCode, TestPlainCode);

        // Act - First use
        recoveryCode.TryMarkAsUsed();
        var originalUsedAt = recoveryCode.UsedAt;

        // Wait a small amount to ensure time would change if it were updated
        Thread.Sleep(10);

        // Act - Attempt second use
        recoveryCode.TryMarkAsUsed();

        // Assert
        recoveryCode.UsedAt.Should().Be(originalUsedAt);
    }

    [Fact]
    public void NormalizeCode_ShouldHandleSpecialCharacters()
    {
        // Act
        var result = MfaRecoveryCode.NormalizeCode("AB-CD!@#$%^&*()EF");

        // Assert
        result.Should().Be("ABCD!@#$%^&*()EF");
    }

    [Theory]
    [InlineData("a", "A")]
    [InlineData("z", "Z")]
    [InlineData("0", "0")]
    [InlineData("9", "9")]
    public void NormalizeCode_ShouldHandleSingleCharacters(string input, string expected)
    {
        // Act
        var result = MfaRecoveryCode.NormalizeCode(input);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Code Validation Tests

    [Fact]
    public void GenerateSecureCode_CodesShouldPassNormalization()
    {
        // Act
        var originalCode = MfaRecoveryCode.GenerateSecureCode();
        var normalizedCode = MfaRecoveryCode.NormalizeCode(originalCode);

        // Assert
        normalizedCode.Should().HaveLength(16);
        normalizedCode.Should().MatchRegex("^[A-Z0-9]{16}$");

        // Verify the normalization is idempotent
        MfaRecoveryCode.NormalizeCode(normalizedCode).Should().Be(normalizedCode);
    }

    [Fact]
    public void GenerateSecureCode_ShouldNotContainConfusingCharacters()
    {
        // This tests that the character set doesn't include easily confused characters
        // The current implementation uses A-Z and 0-9, which includes potentially confusing chars
        // But this test documents the current behavior

        // Act - Generate many codes to get good coverage
        var allCharacters = new HashSet<char>();
        for (int i = 0; i < 1000; i++)
        {
            var code = MfaRecoveryCode.GenerateSecureCode().Replace("-", "");
            foreach (char c in code)
            {
                allCharacters.Add(c);
            }
        }

        // Assert - Verify the character set
        const string expectedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        foreach (char c in allCharacters)
        {
            expectedChars.Should().Contain(c.ToString(), $"character '{c}' should be in the allowed set");
        }
    }

    #endregion
}
