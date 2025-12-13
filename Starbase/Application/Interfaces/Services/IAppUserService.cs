using System.Security.Claims;
using Application.DTOs.Users;
using Application.Models;

namespace Application.Interfaces.Services;

/// <summary>
/// Provides an interface for managing application users, including administrative operations, user updates,
/// and fetching user information. Implementing this interface ensures all necessary operations related to
/// application users are defined and encapsulated.
/// </summary>
public interface IAppUserService
{
    /// <summary>
    /// Retrieves a list of all users in the system with detailed user information.
    /// Intended for administrative use and requires the appropriate user privileges to access.
    /// </summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/> representing the user performing the operation.
    /// This is used to authenticate and authorize the request.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> containing a <see cref="ServiceResponse{T}"/>
    /// that holds a list of <see cref="AppUserDto"/> objects representing the users.
    /// </returns>
    public Task<ServiceResponse<List<AppUserDto>>> AdminGetUsersAsync(ClaimsPrincipal user);

    /// <summary>
    /// Deactivates a user account based on the specified user ID.
    /// This operation is intended for administrative purposes and requires proper authorization.
    /// The user's account will be marked as inactive upon successful execution.
    /// </summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/> representing the administrator initiating the request.
    /// It is used to authenticate and authorize the operation.</param>
    /// <param name="id">The unique <see cref="Guid"/> identifier of the user account to be deactivated.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> containing a <see cref="ServiceResponse{T}"/> with a boolean indicating whether
    /// the deactivation process was successful.
    /// </returns>
    public Task<ServiceResponse<bool>> AdminDeactivateUserAsync(ClaimsPrincipal user, Guid id);

    /// <summary>
    /// Adds a new user to the system with the specified details.
    /// This operation is intended for administrative use and requires appropriate user privileges.
    /// </summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/> representing the user performing the operation.
    /// This is used to authenticate and authorize the request.</param>
    /// <param name="newUser">The <see cref="CreateNewUserDto"/> containing the details of the user to be created,
    /// including username, password, roles, and organization assignment.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> containing a <see cref="ServiceResponse{T}"/>
    /// that encapsulates an <see cref="AppUserDto"/> object representing the newly created user.
    /// </returns>
    public Task<ServiceResponse<AppUserDto>> AdminAddNewUserAsync(ClaimsPrincipal user, CreateNewUserDto newUser);

    /// <summary>
    /// Updates the details of an existing user in the system.
    /// This includes administrative and security checks to ensure the user has appropriate privileges.
    /// </summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/> representing the user performing the operation.
    /// This is used for authentication and authorization validation.</param>
    /// <param name="userToUpdate">An <see cref="AppUserDto"/> containing the updated information
    /// for the user being modified.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> containing a <see cref="ServiceResponse{T}"/> where the result
    /// includes an updated <see cref="AppUserDto"/> object with the applied changes.
    /// </returns>
    public Task<ServiceResponse<AppUserDto>> UpdateUserAsync(ClaimsPrincipal user, AppUserDto userToUpdate);

    /// <summary>
    /// Fetches a list of basic user details for users in the organization tied to the authenticated user.
    /// This method is typically used for scenarios where limited user information is required.
    /// </summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/> representing the currently authenticated user.
    /// Used to identify the organization and validate access permissions.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> containing a <see cref="ServiceResponse{T}"/>
    /// that provides a list of <see cref="BasicAppUserDto"/> objects representing the basic information of users.
    /// </returns>
    public Task<ServiceResponse<List<BasicAppUserDto>>> GetBasicUsersAsync(ClaimsPrincipal user);
}
