using Application.DTOs.Users;
using Application.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.Tests.ValidatorTests;

public class UpdateUserValidatorTests
{
    private readonly UpdateUserValidator _validator = new();

    [Fact]
    public void ValidInput_ShouldPass()
    {
        var dto = new AppUserDto
        {
            Username = "user@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void InvalidEmail_ShouldFail()
    {
        var dto = new AppUserDto
        {
            Username = "invalid-email",
            FirstName = "John",
            LastName = "Doe"
        };

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void EmptyEmail_ShouldFail()
    {
        var dto = new AppUserDto
        {
            Username = "",
            FirstName = "John",
            LastName = "Doe"
        };

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void EmptyFirstName_ShouldFail()
    {
        var dto = new AppUserDto
        {
            Username = "user@example.com",
            FirstName = "",
            LastName = "Doe"
        };

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void EmptyLastName_ShouldFail()
    {
        var dto = new AppUserDto
        {
            Username = "user@example.com",
            FirstName = "John",
            LastName = ""
        };

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }
}
