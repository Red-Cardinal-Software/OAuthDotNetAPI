namespace Domain.Constants;

/// <summary>
/// The <c>PredefinedPrivileges</c> class provides a collection of constant strings
/// that represent predefined privilege keys for different application functionalities.
/// These keys are used to define and manage access control within the system.
/// </summary>
/// <remarks>
/// This static class is categorized into several inner static classes, each representing
/// a specific domain or module in the system, such as authentication, user management,
/// organization management, role and privilege management, general CRUD operations,
/// and system administration. Each constant string within these categories corresponds
/// to a specific privilege action.
/// </remarks>
/// <example>
/// Privilege keys can be used within authorization policies or validations to ensure
/// appropriate access control. This class centralizes the definition of these privileges
/// to provide consistency and reusability across the application.
/// </example>
public static class PredefinedPrivileges
{
    /// <summary>
    /// The <c>Auth</c> class provides a set of predefined privilege keys
    /// associated with authentication functionalities within the system.
    /// </summary>
    /// <remarks>
    /// This static class contains constants that define specific authentication-related actions,
    /// such as user login and impersonation. These constants can be used to enforce
    /// access control and define policies where authentication features are required.
    /// </remarks>
    public static class Auth
    {
        public const string Login = "auth:login";
        public const string Impersonate = "auth:impersonate";
    }

    /// <summary>
    /// The <c>UserManagement</c> class contains constant strings that represent privilege keys
    /// specific to user management actions within the application. These privileges are used
    /// to control and enforce access restrictions for managing user-related operations.
    /// </summary>
    /// <remarks>
    /// This static class defines permissions required for various user management functionalities,
    /// including viewing users, creating new users, updating existing users, deactivating users,
    /// and assigning roles. Each privilege key is intended to be utilized in access control mechanisms
    /// to ensure users have appropriate permissions for specific actions.
    /// </remarks>
    public static class UserManagement
    {
        public const string View = "user:view";
        public const string ViewBasic = "user:view-basic";
        public const string Create = "user:create";
        public const string Update = "user:update";
        public const string Deactivate = "user:deactivate";
        public const string AssignRole = "user:assign-role";
    }

    /// <summary>
    /// The <c>OrganizationManagement</c> class provides a collection of predefined privilege keys
    /// related to managing organizations within the system.
    /// These privileges define access control boundaries for organization-related operations.
    /// </summary>
    /// <remarks>
    /// This static class defines constants representing specific actions that can be performed
    /// on organizations, such as viewing, creating, updating, and deactivating. These keys are
    /// intended to be used in access control policies to enforce proper authorization checks.
    /// </remarks>
    public static class OrganizationManagement
    {
        public const string View = "org:view";
        public const string Create = "org:create";
        public const string Update = "org:update";
        public const string Deactivate = "org:deactivate";
    }

    /// <summary>
    /// The <c>RoleAndPrivilegeManagement</c> class provides predefined privilege keys
    /// specifically associated with managing roles and their assigned privileges within the system.
    /// </summary>
    /// <remarks>
    /// This static class defines constant strings that represent actions related to role
    /// management, such as viewing, creating, updating, deactivating roles, and assigning privileges.
    /// These privilege keys are utilized to enforce role-based access control policies and
    /// ensure only authorized users can perform these operations.
    /// </remarks>
    public static class RoleAndPrivilegeManagement
    {
        public const string View = "role:view";
        public const string Create = "role:create";
        public const string Update = "role:update";
        public const string Deactivate = "role:deactivate";
        public const string AssignPrivilege = "role:assign";
    }

    /// <summary>
    /// The <c>GeneralCrud</c> class defines a set of constant privilege keys
    /// related to basic Create, Read, Update, and Delete (CRUD) operations on data.
    /// </summary>
    /// <remarks>
    /// This static class is part of the broader <c>PredefinedPrivileges</c> structure
    /// and is specifically focused on providing consistent privilege definitions
    /// for general data management operations. Applications can use these keys
    /// to enforce access control policies in CRUD scenarios.
    /// </remarks>
    public static class GeneralCrud
    {
        public const string View = "data:view";
        public const string Create = "data:create";
        public const string Update = "data:update";
        public const string Delete = "data:delete";
    }

    /// <summary>
    /// The <c>SystemAdministration</c> class provides a set of predefined privilege constants
    /// for actions related to managing system-level functionalities and configurations.
    /// </summary>
    /// <remarks>
    /// This static class is part of the <c>PredefinedPrivileges</c> hierarchy and includes constants
    /// representing privileges for viewing system metrics, managing tenants, handling application secrets,
    /// and executing seeding operations. These privileges are essential for access control and security
    /// enforcement at the system administration level.
    /// </remarks>
    public static class SystemAdministration
    {
        public const string Metrics = "system:metrics:view";
        public const string ManageTenants = "system:tenant:manage";
        public const string Secrets = "system:secrets:manage";
        public const string SeedingExecute = "system:seeding:execute";
    }
}
