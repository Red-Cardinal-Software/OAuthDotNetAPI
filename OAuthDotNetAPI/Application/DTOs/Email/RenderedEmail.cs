namespace Application.DTOs.Email;

public class RenderedEmail
{
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public bool IsHtml { get; init; }
}