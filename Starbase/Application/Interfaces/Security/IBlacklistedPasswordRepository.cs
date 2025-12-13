namespace Application.Interfaces.Security;

/// <summary>
/// Provides an abstraction for accessing and verifying passwords against a blacklist.
/// </summary>
public interface IBlacklistedPasswordRepository
{
    /// <summary>
    /// Checks asynchronously if the provided password is included in the list of blacklisted passwords.
    /// </summary>
    /// <param name="password">The password to check against the blacklist.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the password is blacklisted (true if blacklisted; otherwise, false).</returns>
    public Task<bool> IsPasswordBlacklistedAsync(string password);
}
