using Application.DTOs.Email;

namespace Application.Common.Email;

/// <summary>
/// Provides a mechanism for rendering email templates into fully formatted emails.
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <summary>
    /// Renders an email template into a fully formatted email based on the provided template key and model.
    /// </summary>
    /// <typeparam name="TModel">The type of the model used for token replacement in the template.</typeparam>
    /// <param name="templateKey">The key identifying the email template to be rendered.</param>
    /// <param name="model">The model object used to replace tokens in the email template.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the rendered email.</returns>
    Task<RenderedEmail> RenderAsync<TModel>(string templateKey, TModel model);
}
