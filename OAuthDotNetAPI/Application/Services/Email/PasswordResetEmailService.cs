using Application.Common.Constants;
using Application.Common.Email;
using Application.DTOs.Email;
using Application.Interfaces.Services;
using Domain.Entities.Identity;

namespace Application.Services.Email;

public class PasswordResetEmailService(IEmailTemplateRenderer templateRenderer, IEmailService emailService) : IPasswordResetEmailService
{
    public async Task SendPasswordResetEmail(Domain.Entities.Identity.AppUser user, PasswordResetToken token)
    {
        var email = await templateRenderer.RenderAsync(EmailTemplateKeys.PasswordReset, new PasswordResetEmailModel
        {
            FirstName = user.FirstName,
            ResetToken = token.Id.ToString()
        });
        await emailService.SendEmailAsync(user.Username, email);
    }
}