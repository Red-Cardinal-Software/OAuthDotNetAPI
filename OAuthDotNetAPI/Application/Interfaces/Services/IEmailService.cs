using Application.DTOs.Email;

namespace Application.Interfaces.Services;

/// <summary>
/// Defines the contract for an email service that handles sending emails in the application.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously to the specified recipient using a pre-rendered email object.
    /// </summary>
    /// <param name="to">The email address of the recipient.</param>
    /// <param name="email">An object containing the pre-rendered email subject, body content, and HTML format indicator.</param>
    /// <returns>A task representing the asynchronous operation of sending the email.</returns>
    Task SendEmailAsync(string to, RenderedEmail email);
}
