namespace Application.Common.Constants;

public static class ServiceResponseConstants
{
    // Statuses
    public const int Ok = 1000;
    public const int Unauthorized = 1403;
    public const int GeneralError = 1001;
    public const int EntityAlreadyExists = 2601;
    public const int EntityDoesNotExist = 1404;
    public const int DatabaseError = 9000;
    public const int QuickbooksTimeSyncEmployeeNotSynced = 2000;

    // // Messages

    // Generic
    public const string NameIsRequired = "Name is required";

    // User
    public const string PasswordIsRequired = "Password is required";
    public const string AppUserNotFound = "User was not found";
    public const string UserUnauthorized = "User is not authorized";
    public const string EmailPasswordResetSent = "If this E-mail address exists in the system, then we have sent a password reset link to that E-mail";
    public const string TokenNotFound = "Token not found";
    public const string PasswordMustNotBeEmpty = "Password must not be empty";
    public const string UserAlreadyExists = "A user with this e-mail is already registered for this organization";
    public const string PasswordDoesNotMeetMinimumLengthRequirements = "Password does not meet minimum length requirements of {0}.";
    public const string PasswordExceedsMaximumLengthRequirements = "Password must be less than {0} characters";
    public const string PasswordMustBeDifferentFromCurrent = "Password must be different from current";
    public const string PasswordIsBlacklisted = "Password is too common";
    public const string UserNotFound = "User was not found";
    public const string CannotRemoveAdminForSelf = "You cannot remove admin role from yourself";
    public const string SomeEmployeesAreNotSynced = "Some employees are not synced, not all time entries could be synced";
    public const string EmployeeNotActive = "Employee is not active";
    public const string UserNotRequiredToResetPassword = "User is not required to reset password";
    public const string EmailNotValid = "Email for username is not valid";

    // Auth
    public const string RefreshTokenExpired = "Refresh token expired";
    public const string UnableToGenerateRefreshToken = "Unable to generate refresh token";
    public const string UsernameOrPasswordIncorrect = "Username or password incorrect";
    public const string TokenMismatch = "Token mismatch";
    public const string InvalidPasswordResetToken = "Invalid password reset token";
    public const string RefreshTokenAlreadyClaimed = "Refresh token already claimed";
    public const string UserLoggedOut = "User Logged Out";


    // Organization
    public const string YouAreNotPartOfThisOrganization = "You are not part of this Organization";
    public const string OrganizationDoesNotExist = "Organization does not exist";
    public const string PaymentMethodNotRecognizedForOrganization = "This payment profile does not exist for the Organization";
    public const string OrganizationIsRequired = "Organization is required.";
}
