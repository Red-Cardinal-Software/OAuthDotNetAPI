using Application.Common.Constants;
using Application.Common.Interfaces;
using Application.DTOs.Users;
using Application.Interfaces.Repositories;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Validator class for validating properties of a new user creation request. Extends FluentValidation's <see cref="AbstractValidator{T}" />.
/// </summary>
/// <remarks>
/// Validates the following:
/// - First name and last name must not be empty.
/// - Username must be a valid email address and unique within the organization.
/// Password validation is handled separately in the associated service.
/// </remarks>
public class NewUserValidator : AbstractValidator<CreateNewUserDto>
{
    public NewUserValidator(IAppUserRepository userRepository, IUserContext userContext)
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();

        RuleFor(x => x.Username)
            .EmailAddress()
            .WithMessage(ServiceResponseConstants.EmailNotValid)
            .MustAsync(async (username, _) =>
            !await userRepository.DoesUserExistForOrgAsync(username, userContext.GetOrganizationId()))
            .WithMessage(ServiceResponseConstants.UserAlreadyExists);

        // Password Validation is done in the service
    }
}
