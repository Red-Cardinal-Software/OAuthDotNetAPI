using Application.DTOs.Email;
using Application.Interfaces.Services;

namespace Application.Services.Email;

/// <summary>
/// A placeholder implementation of the <see cref="IEmailService"/> interface that has not been implemented yet.
/// </summary>
/// <remarks>
/// All methods in this service throw a <see cref="NotImplementedException"/>. This class is intended to be used
/// as a placeholder or during development to highlight missing functionality. Replace with your own implementation
/// </remarks>
public class NotImplementedEmailService : IEmailService
{
    public Task SendEmailAsync(string to, RenderedEmail email)
    {
        throw new NotImplementedException();
    }
}
