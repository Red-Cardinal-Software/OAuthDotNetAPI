using Application.DTOs.Users;
using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Represents a validator for updating user data.
/// </summary>
/// <remarks>
/// This validator ensures that user update data follows specific rules:
/// - The username must be in a valid email address format and cannot be empty.
/// - The first name must not be empty.
/// - The last name must not be empty.
/// </remarks>
public class UpdateUserValidator : AbstractValidator<AppUserDto>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Username).EmailAddress().NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
    }
}