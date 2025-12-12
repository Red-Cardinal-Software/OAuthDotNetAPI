namespace Domain.Entities.Configuration;

/// <summary>
/// Represents a template for emails within the system.
/// </summary>
/// <remarks>
/// The <see cref="EmailTemplate"/> class is used for managing the data and structure of email content,
/// including the subject, body, and whether the body is in HTML format.
/// It includes functionality for creating and updating email templates and ensuring data validation.
/// </remarks>
public class EmailTemplate
{
    /// <summary>
    /// Gets the unique identifier for the email template.
    /// </summary>
    /// <remarks>
    /// The identifier is a GUID that is automatically generated when a new instance
    /// of the <see cref="EmailTemplate"/> class is created.
    /// </remarks>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the unique key associated with the email template.
    /// </summary>
    /// <remarks>
    /// The key is a string identifier provided during the creation of an <see cref="EmailTemplate"/> instance.
    /// It is used to reference the email template for retrieval and management purposes.
    /// </remarks>
    public string Key { get; private set; } = null!;

    /// <summary>
    /// Gets the subject of the email template.
    /// </summary>
    /// <remarks>
    /// The subject defines the title or topic of the email and is displayed to the recipient
    /// in their email client. It cannot be null or whitespace during instantiation or updates.
    /// </remarks>
    public string Subject { get; private set; } = null!;

    /// <summary>
    /// Gets the content of the email template.
    /// </summary>
    /// <remarks>
    /// The body contains the main message or content of the email. It can include plain text
    /// or HTML, depending on the value of the <see cref="IsHtml"/> property.
    /// </remarks>
    public string Body { get; private set; } = null!;

    /// <summary>
    /// Indicates whether the email body content is formatted as HTML.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c>, the email body will be treated as HTML, allowing support for
    /// rich formatting, links, and other HTML features. When set to <c>false</c>, the body
    /// will be treated as plain text.
    /// </remarks>
    public bool IsHtml { get; private set; }

    /// <summary>
    /// Constructor for EF Core
    /// </summary>
    protected EmailTemplate() { }

    /// <summary>
    /// Represents an email template used within the system, including its key,
    /// subject, body content, and a flag indicating whether the content is in HTML format.
    /// </summary>
    public EmailTemplate(string key, string subject, string body, bool isHtml = true)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key), "Email template key cannot be null or whitespace.");

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentNullException(nameof(subject), "Subject cannot be null or whitespace.");

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentNullException(nameof(body), "Body cannot be null or whitespace.");

        Id = Guid.NewGuid();
        Key = key;
        Subject = subject;
        Body = body;
        IsHtml = isHtml;
    }

    /// <summary>
    /// Updates the content of the email template, including its subject and body.
    /// </summary>
    /// <param name="subject">The updated subject for the email template. Cannot be null or whitespace.</param>
    /// <param name="body">The updated body for the email template. Cannot be null or whitespace.</param>
    public void UpdateContent(string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentNullException(nameof(subject), "Subject cannot be null or whitespace.");
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentNullException(nameof(body), "Body cannot be null or whitespace.");

        Subject = subject;
        Body = body;
    }
}
