namespace Application.DTOs.Email;

/// <summary>
/// Represents the model for a password reset email, including the user's first name
/// and the token required to reset the password.
/// Can be customized to whatever is needed in your application
/// </summary>
public class PasswordResetEmailModel
{
    public string FirstName { get; set; } = string.Empty;
    public string ResetToken { get; set; } = string.Empty;
}
