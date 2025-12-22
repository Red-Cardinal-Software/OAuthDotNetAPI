namespace Domain.Entities.Audit;

/// <summary>
/// Specific actions within audit events.
/// </summary>
public enum AuditAction
{
    // Authentication actions
    LoginSuccess = 100,
    LoginFailed = 101,
    Logout = 102,
    TokenRefresh = 103,
    TokenRevoked = 104,

    // MFA actions
    MfaSetupStarted = 200,
    MfaSetupCompleted = 201,
    MfaSetupFailed = 202,
    MfaVerificationSuccess = 203,
    MfaVerificationFailed = 204,
    MfaMethodRemoved = 205,
    MfaRecoveryCodeUsed = 206,
    MfaRecoveryCodesRegenerated = 207,

    // Password actions
    PasswordChanged = 300,
    PasswordResetRequested = 301,
    PasswordResetCompleted = 302,
    PasswordResetFailed = 303,
    ForcePasswordResetRequired = 304,

    // User management actions
    UserCreated = 400,
    UserUpdated = 401,
    UserDeactivated = 402,
    UserReactivated = 403,
    UserDeleted = 404,

    // Role/privilege actions
    RoleAssigned = 500,
    RoleRemoved = 501,
    RoleCreated = 502,
    RoleUpdated = 503,
    RoleDeleted = 504,
    PrivilegeGranted = 505,
    PrivilegeRevoked = 506,

    // Data operations
    DataCreated = 600,
    DataRead = 601,
    DataUpdated = 602,
    DataDeleted = 603,

    // Security events
    AccountLocked = 700,
    AccountUnlocked = 701,
    RateLimitExceeded = 702,
    SuspiciousActivity = 703,
    AccessDenied = 704,
    InvalidToken = 705,

    // System events
    ConfigurationChanged = 800,
    MaintenanceStarted = 801,
    MaintenanceCompleted = 802,
    SystemHealthCheck = 803
}