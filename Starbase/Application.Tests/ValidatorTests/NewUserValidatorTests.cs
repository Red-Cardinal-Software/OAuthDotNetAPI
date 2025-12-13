using Application.Common.Constants;
using Application.Common.Interfaces;
using Application.DTOs.Users;
using Application.Interfaces.Repositories;
using Application.Validators;
using FluentAssertions;
using Moq;
using Xunit;

namespace Application.Tests.ValidatorTests;

public class NewUserValidatorTests
{
    private readonly Mock<IAppUserRepository> _userRepository = new();
    private readonly Mock<IUserContext> _userContext = new();
    private readonly NewUserValidator _validator;

    public NewUserValidatorTests()
    {
        _userContext.Setup(x => x.GetOrganizationId()).Returns(Guid.NewGuid());
        _validator = new NewUserValidator(_userRepository.Object, _userContext.Object);
    }

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var dto = new CreateNewUserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Username = "john.doe@example.com",
            Password = ""
        };

        _userRepository.Setup(x =>
            x.DoesUserExistForOrgAsync(dto.Username, It.IsAny<Guid>())
        ).ReturnsAsync(false);

        var result = await _validator.ValidateAsync(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyFirstName_ShouldFail()
    {
        var dto = new CreateNewUserDto
        {
            FirstName = "",
            LastName = "Doe",
            Username = "john.doe@example.com",
            Password = ""
        };

        var result = await _validator.ValidateAsync(dto);
        result.Errors.Should().Contain(x => x.PropertyName == "FirstName");
    }

    [Fact]
    public async Task InvalidEmail_ShouldFail()
    {
        var dto = new CreateNewUserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Username = "invalid-email",
            Password = ""
        };

        var result = await _validator.ValidateAsync(dto);
        result.Errors.Should().Contain(x =>
            x.PropertyName == "Username" &&
            x.ErrorMessage == ServiceResponseConstants.EmailNotValid);
    }

    [Fact]
    public async Task DuplicateUsername_ShouldFail()
    {
        var orgId = Guid.NewGuid();
        _userContext.Setup(x => x.GetOrganizationId()).Returns(orgId);
        _userRepository.Setup(x => x.DoesUserExistForOrgAsync("john.doe@example.com", orgId)).ReturnsAsync(true);

        var dto = new CreateNewUserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Username = "john.doe@example.com",
            Password = ""
        };

        var result = await _validator.ValidateAsync(dto);
        result.Errors.Should().Contain(x =>
            x.PropertyName == "Username" &&
            x.ErrorMessage == ServiceResponseConstants.UserAlreadyExists);
    }
}
