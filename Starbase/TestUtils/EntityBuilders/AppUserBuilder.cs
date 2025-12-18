using Application.Interfaces.Security;
using Domain.Entities.Identity;
using Infrastructure.Security;
using TestUtils.Utilities;

namespace TestUtils.EntityBuilders;

public class AppUserBuilder
{
    private readonly IPasswordHasher _passwordHasher = new BcryptPasswordHasher();
    private Guid _orgId = TestConstants.Ids.OrganizationId;
    private string _email = TestConstants.Emails.Default;
    private string _password = TestConstants.Passwords.Default;
    private string _firstName = TestConstants.Names.DefaultFirstName;
    private string _lastName = TestConstants.Names.DefaultLastName;
    private bool _forceResetPassword = true;

    public AppUserBuilder WithOrganizationId(Guid orgId)
    {
        _orgId = orgId;
        return this;
    }

    public AppUserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public AppUserBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public AppUserBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    public AppUserBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public AppUserBuilder WithForceResetPassword(bool forceResetPassword)
    {
        _forceResetPassword = forceResetPassword;
        return this;
    }

    public AppUser Build()
    {
        var hashedPassword = _passwordHasher.Hash(_password);
        return new AppUser(_email, hashedPassword, _firstName, _lastName, _orgId, _forceResetPassword);
    }
}
