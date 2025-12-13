using Domain.Entities.Configuration;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Interface defining the contract for interacting with email templates in the system.
/// </summary>
public interface IEmailTemplateRepository
{
    /// <summary>
    /// Retrieves an email template by its unique key.
    /// </summary>
    /// <param name="key">The unique key identifying the email template to retrieve.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the <see cref="EmailTemplate"/>
    /// matching the provided key. Null is returned if no matching template is found.
    /// </returns>
    Task<EmailTemplate?> GetEmailTemplateByKeyAsync(string key);
}
