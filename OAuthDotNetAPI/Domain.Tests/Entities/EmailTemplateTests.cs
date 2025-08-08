using Domain.Entities.Configuration;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Entities;

public class EmailTemplateTests
{
    [Fact]
    public void Constructor_WithValidInputs_ShouldSetProperties()
    {
        // Arrange
        var key = "welcome_email";
        var subject = "Welcome!";
        var body = "<p>Thanks for joining us!</p>";
        var isHtml = true;

        // Act
        var template = new EmailTemplate(key, subject, body, isHtml);

        // Assert
        template.Key.Should().Be(key);
        template.Subject.Should().Be(subject);
        template.Body.Should().Be(body);
        template.IsHtml.Should().Be(isHtml);
        template.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidKey_ShouldThrow(string? invalidKey)
    {
        // Act
        var act = () => new EmailTemplate(invalidKey!, "subject", "body");

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*key*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidSubject_ShouldThrow(string? invalidSubject)
    {
        // Act
        var act = () => new EmailTemplate("key", invalidSubject!, "body");

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*subject*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithInvalidBody_ShouldThrow(string? invalidBody)
    {
        // Act
        var act = () => new EmailTemplate("key", "subject", invalidBody!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*body*");
    }

    [Fact]
    public void UpdateContent_WithValidInputs_ShouldUpdateSubjectAndBody()
    {
        // Arrange
        var template = new EmailTemplate("key", "Old Subject", "Old Body");

        // Act
        template.UpdateContent("New Subject", "New Body");

        // Assert
        template.Subject.Should().Be("New Subject");
        template.Body.Should().Be("New Body");
    }

    [Theory]
    [InlineData(null, "Body")]
    [InlineData("", "Body")]
    [InlineData(" ", "Body")]
    [InlineData("Subject", null)]
    [InlineData("Subject", "")]
    [InlineData("Subject", " ")]
    public void UpdateContent_WithInvalidInputs_ShouldThrow(string? subject, string? body)
    {
        // Arrange
        var template = new EmailTemplate("key", "subject", "body");

        // Act
        var act = () => template.UpdateContent(subject!, body!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
