using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Domain.Entities.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EmailTemplateRepository(ICrudOperator<EmailTemplate> emailTemplateCrudOperator) : IEmailTemplateRepository
{
    public Task<EmailTemplate?> GetEmailTemplateByKeyAsync(string key) =>
        emailTemplateCrudOperator.GetAll().FirstOrDefaultAsync(et => et.Key == key);
}