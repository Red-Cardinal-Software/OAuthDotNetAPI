using Domain.Constants;

namespace Domain.Authorization;

/// <summary>
/// Represents a specific privilege configuration which encapsulates details about the privilege
/// such as its name, description, and whether it is assigned by default to certain roles or users.
/// </summary>
/// <remarks>
/// This class provides a structured definition for privileges within the system, allowing precise control
/// over access permissions. Each instance of this class contains the privilege's unique identifier,
/// descriptive information, and flags indicating its default assignment to system roles (e.g., admin, user).
/// </remarks>
/// <example>
/// This record is used to define granular permissions that are consumed throughout the application for
/// authorization and role-based access control checks.
/// </example>
public record PrivilegeDefinition(
    string Name,
    string Description,
    bool IsSystemDefault = true,
    bool IsAdminDefault = false,
    bool IsUserDefault = false);

/// <summary>
/// Defines a static set of predefined privileges for the system, representing various permissions
/// that can be applied to users, roles, or organizations.
/// </summary>
/// <remarks>
/// Each privilege definition includes details such as the privilege identifier, description,
/// and configuration flags such as availability, default inclusion, and eligibility for roles or users.
/// </remarks>
/// <example>
/// This class is a central repository for all privilege definitions that may be used across the application
/// to enforce authorization policies or validate user permissions.
/// </example>
public static class PrivilegeDefinitions
{
    /// <summary>
    /// Holds all predefined system privileges with default assignments for seeding and role generation.
    /// </summary>
    public static readonly PrivilegeDefinition[] All =
    [
        new(PredefinedPrivileges.Auth.Login, "Allows users to log in", true, true, true),
        new(PredefinedPrivileges.Auth.Impersonate, "Allows users to impersonate other users"),
        new(PredefinedPrivileges.UserManagement.View, "View users in the organization", true, true),
        new(PredefinedPrivileges.UserManagement.ViewBasic, "View just basic user info for users in the org", true, true),
        new(PredefinedPrivileges.UserManagement.Create, "Create new users in the organization", true, true),
        new(PredefinedPrivileges.UserManagement.AssignRole, "Ability to assign roles to users", true, true),
        new(PredefinedPrivileges.UserManagement.Deactivate, "Ability to deactivate users", true, true),
        new(PredefinedPrivileges.UserManagement.Update, "Ability to update users", true, true),
        new(PredefinedPrivileges.OrganizationManagement.Create, "Ability to create new organizations"),
        new(PredefinedPrivileges.OrganizationManagement.Update, "Ability to update organizations"),
        new(PredefinedPrivileges.OrganizationManagement.Deactivate, "Ability to deactivate organizations"),
        new(PredefinedPrivileges.OrganizationManagement.View, "Ability to view organizations"),
        new(PredefinedPrivileges.RoleAndPrivilegeManagement.Create, "Ability to create new roles for organization", true, true),
        new(PredefinedPrivileges.RoleAndPrivilegeManagement.Update, "Ability to update roles for organization", true, true),
        new(PredefinedPrivileges.RoleAndPrivilegeManagement.View, "Ability to view roles for organization", true, true),
        new(PredefinedPrivileges.RoleAndPrivilegeManagement.Deactivate, "Ability to deactivate roles for organization", true, true),
        new(PredefinedPrivileges.RoleAndPrivilegeManagement.AssignPrivilege, "Ability to assign roles to users", true, true),
        new(PredefinedPrivileges.GeneralCrud.View, "Ability to view data", true, true, true),
        new(PredefinedPrivileges.GeneralCrud.Create, "Ability to create data", true, true),
        new(PredefinedPrivileges.GeneralCrud.Update, "Ability to update data", true, true),
        new(PredefinedPrivileges.GeneralCrud.Delete, "Ability to delete data", true, true),
        new(PredefinedPrivileges.SystemAdministration.ManageTenants, "Ability to manage tenants"),
        new(PredefinedPrivileges.SystemAdministration.Metrics, "Ability to view metrics"),
        new(PredefinedPrivileges.SystemAdministration.Secrets, "Ability to manage secrets and config for system"),
        new(PredefinedPrivileges.SystemAdministration.SeedingExecute, "Ability to execute seeding")
    ];
}
