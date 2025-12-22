namespace Domain.Entities.Audit;

/// <summary>
/// Categories of audit events for compliance tracking.
/// </summary>
public enum AuditEventType
{
    /// <summary>
    /// Authentication events: login, logout, token refresh.
    /// </summary>
    Authentication = 1,

    /// <summary>
    /// Authorization events: access granted, access denied, privilege checks.
    /// </summary>
    Authorization = 2,

    /// <summary>
    /// MFA events: setup, verification, recovery code usage.
    /// </summary>
    MfaOperation = 3,

    /// <summary>
    /// User management: create, update, deactivate users.
    /// </summary>
    UserManagement = 4,

    /// <summary>
    /// Role and privilege changes.
    /// </summary>
    RoleManagement = 5,

    /// <summary>
    /// Password operations: reset, change, policy violations.
    /// </summary>
    PasswordOperation = 6,

    /// <summary>
    /// Data access: reads of sensitive data.
    /// </summary>
    DataAccess = 7,

    /// <summary>
    /// Data modification: creates, updates, deletes.
    /// </summary>
    DataChange = 8,

    /// <summary>
    /// Security events: lockouts, rate limits, suspicious activity.
    /// </summary>
    SecurityEvent = 9,

    /// <summary>
    /// System events: configuration changes, maintenance operations.
    /// </summary>
    SystemEvent = 10
}