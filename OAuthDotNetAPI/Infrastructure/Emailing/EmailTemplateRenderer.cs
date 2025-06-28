using System.Reflection;
using Application.Common.Email;
using Application.DTOs.Email;
using Application.Interfaces.Repositories;
using Domain.Entities.Configuration;

namespace Infrastructure.Emailing;

/// <summary>
/// Provides functionality to render email templates into fully constructed emails with dynamic content.
/// </summary>
public class EmailTemplateRenderer(IEmailTemplateRepository templateRepository) : IEmailTemplateRenderer
{
    public async Task<RenderedEmail> RenderAsync<TModel>(string templateKey, TModel model)
    {
        var template = await GetTemplateAsync(templateKey);
        return new RenderedEmail
        {
            Subject = ReplaceTokens(template.Subject, model),
            Body = ReplaceTokens(template.Body, model),
            IsHtml = template.IsHtml
        };
    }

    /// <summary>
    /// Retrieves an email template by a specified template key.
    /// </summary>
    /// <param name="templateKey">The unique key identifying the email template to retrieve.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the <see cref="EmailTemplate"/>
    /// matching the provided key. Throws an exception if the template is not found.
    /// </returns>
    private async Task<EmailTemplate> GetTemplateAsync(string templateKey)
    {
        var template = await templateRepository.GetEmailTemplateByKeyAsync(templateKey);
        
        return template ?? throw new Exception($"Template with key {templateKey} not found");
    }

    /// <summary>
    /// Replaces placeholders in the provided text with corresponding values from the given model.
    /// </summary>
    /// <param name="text">The string containing placeholders to be replaced.</param>
    /// <param name="model">The model object containing values to replace the placeholders. Properties of this model are matched to placeholders by name.</param>
    /// <typeparam name="TModel">The type of the model object.</typeparam>
    /// <returns>
    /// A string with all placeholders replaced by their corresponding values from the model.
    /// If the model is null or no matching property is found for a placeholder, the original placeholder remains in the string.
    /// </returns>
    private static string ReplaceTokens<TModel>(string text, TModel model)
    {
        if (model == null) return text;

        var props = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name, p => p.GetValue(model)?.ToString() ?? string.Empty);

        foreach (var (key, value) in props)
        {
            text = text.Replace($"{{{key}}}", value);
        }

        return text;
    }
}