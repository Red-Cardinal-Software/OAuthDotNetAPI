using System.Security.Claims;
using Application.Common.Utilities;
using Microsoft.Extensions.Logging;

namespace Application.Logging;

/// <summary>
/// Provides helper methods for logging messages with additional structured context.
/// Handles just about any type of logging needed to make a one-stop shop for logging in the whole application.
/// </summary>
/// <typeparam name="T">The type of the logger's context, typically the type of the calling class.</typeparam>
public class LogContextHelper<T>(ILogger<T> logger)
{
    /// <summary>
    /// Begins a structured logging scope using user claims.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <returns>An IDisposable logging scope.</returns>
    private IDisposable BeginUserScope(ClaimsPrincipal user)
    {
        var context = new Dictionary<string, object?>
        {
            ["UserId"] = RoleUtility.GetUserIdFromClaims(user),
            ["OrgId"] = RoleUtility.GetOrgIdFromClaims(user),
            ["Username"] = RoleUtility.GetUserNameFromClaim(user)
        };

        return logger.BeginScope(context) ?? NullScope.Instance;
    }

    /// <summary>
    /// Logs an informational message with structured context from the provided user claims.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal representing the user performing the action.</param>
    /// <param name="builder">The structured log builder to generate the message from.</param>

    public void InfoWithContext(ClaimsPrincipal user, StructuredLogBuilder builder) =>
        LogWithContext(user, builder, LogLevel.Information);

    /// <summary>
    /// Logs a warning message with structured context from the provided user claims.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal representing the user performing the action.</param>
    /// <param name="builder">The structured log builder to generate the message from.</param>
    public void WarningWithContext(ClaimsPrincipal user, StructuredLogBuilder builder) =>
        LogWithContext(user, builder, LogLevel.Warning);

    /// <summary>
    /// Logs a critical message with structured context from the provided user claims.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal representing the user performing the action.</param>
    /// <param name="builder">The structured log builder to generate the message from.</param>
    public void CriticalWithContext(ClaimsPrincipal user, StructuredLogBuilder builder) =>
        LogWithContext(user, builder, LogLevel.Critical);

    /// <summary>
    /// Logs an error message with structured context from the provided user claims and optional exception details.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal representing the user performing the action.</param>
    /// <param name="builder">The builder used to construct the structured log message.</param>
    /// <param name="ex">An optional exception to include in the log message.</param>
    public void ErrorWithContext(ClaimsPrincipal user, StructuredLogBuilder builder, Exception? ex = null) =>
        LogWithContext(user, builder, LogLevel.Error, false, ex);

    /// <summary>
    /// Logs a debug message with structured context from the provided user claims.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal representing the user performing the action.</param>
    /// <param name="builder">The structured log builder to generate the message from.</param>
    public void DebugWithContext(ClaimsPrincipal user, StructuredLogBuilder builder) =>
        LogWithContext(user, builder, LogLevel.Debug);

    /// <summary>
    /// Logs an informational message with structured context from the provided user claims as JSON.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal representing the user performing the action.</param>
    /// <param name="builder">The structured log builder to generate the message from.</param>
    public void InfoWithContextJson(ClaimsPrincipal user, StructuredLogBuilder builder) =>
        LogWithContext(user, builder, LogLevel.Information, true);

    /// <summary>
    /// Logs a warning message with structured context from the provided user claims as JSON.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal representing the user performing the action.</param>
    /// <param name="builder">The structured log builder to generate the message from.</param>
    public void WarningWithContextJson(ClaimsPrincipal user, StructuredLogBuilder builder) =>
        LogWithContext(user, builder, LogLevel.Warning, true);

    /// <summary>
    /// Logs a critical message with structured context from the provided user claims as JSON.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal representing the user performing the action.</param>
    /// <param name="builder">The structured log builder to generate the message from.</param>
    public void CriticalWithContextJson(ClaimsPrincipal user, StructuredLogBuilder builder) =>
        LogWithContext(user, builder, LogLevel.Critical, true);

    /// <summary>
    /// Logs an error message with structured context from the provided user claims as a JSON-formatted log entry.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal representing the user performing the action.</param>
    /// <param name="builder">The structured log builder used to generate the log message.</param>
    /// <param name="ex">An optional exception to include in the log entry.</param>
    public void ErrorWithContextJson(ClaimsPrincipal user, StructuredLogBuilder builder, Exception? ex = null) =>
        LogWithContext(user, builder, LogLevel.Error, true, ex);

    /// <summary>
    /// Logs a debug message with structured context from the provided user claims as JSON.
    /// </summary>
    /// <param name="user">The ClaimsPrincipal representing the user performing the action.</param>
    /// <param name="builder">The structured log builder to generate the message from.</param>
    public void DebugWithContextJson(ClaimsPrincipal user, StructuredLogBuilder builder) =>
        LogWithContext(user, builder, LogLevel.Debug, true);

    /// <summary>
    /// Logs an informational message with structured context. Meant for areas not with an associated user.
    /// </summary>
    /// <param name="builder">The structured log builder to generate the message from.</param>
    public void Info(StructuredLogBuilder builder) =>
        LogWithContext(null, builder, LogLevel.Information);

    /// <summary>
    /// Logs a warning message with structured context. Meant for areas not with an associated user.
    /// </summary>
    /// <param name="builder">The structured log builder to generate the message from.</param>
    public void Warning(StructuredLogBuilder builder) =>
        LogWithContext(null, builder, LogLevel.Warning);

    /// <summary>
    /// Logs a critical message with structured context. Meant for areas not with an associated user.
    /// </summary>
    /// <param name="builder">The structured log builder to generate the message from.</param>
    public void Critical(StructuredLogBuilder builder) =>
        LogWithContext(null, builder, LogLevel.Critical);


    /// <summary>
    /// Logs an error message with structured context. Intended for non-user-associated areas.
    /// </summary>
    /// <param name="builder">The structured log builder used to generate the log message.</param>
    /// <param name="ex">An optional exception to include with the log entry.</param>
    public void Error(StructuredLogBuilder builder, Exception? ex = null) =>
        LogWithContext(null, builder, LogLevel.Error, false, ex);

    /// <summary>
    /// Logs a debug message with structured context. Meant for areas not with an associated user.
    /// </summary>
    /// <param name="builder">The structured log builder to generate the message from.</param>
    public void Debug(StructuredLogBuilder builder) =>
        LogWithContext(null, builder, LogLevel.Debug);

    /// <summary>
    /// Logs an informational message with structured context as JSON. Meant for areas not with an associated user.
    /// </summary>
    /// <param name="builder">The structured log builder to generate the message from.</param>
    public void InfoJson(StructuredLogBuilder builder) =>
        LogWithContext(null, builder, LogLevel.Information, true);

    /// <summary>
    /// Logs a warning message with structured context as JSON. Meant for areas not with an associated user.
    /// </summary>
    /// <param name="builder">The structured log builder to generate the message from.</param>
    public void WarningJson(StructuredLogBuilder builder) =>
        LogWithContext(null, builder, LogLevel.Warning, true);

    /// <summary>
    /// Logs a critical message with structured context as JSON. Meant for areas not with an associated user.
    /// </summary>
    /// <param name="builder">The structured log builder to generate the message from.</param>
    public void CriticalJson(StructuredLogBuilder builder) =>
        LogWithContext(null, builder, LogLevel.Critical, true);

    /// <summary>
    /// Logs an error message with structured context as JSON. Meant for areas where there is no associated user.
    /// </summary>
    /// <param name="builder">The structured log builder used to generate the message.</param>
    /// <param name="ex">The exception to include in the log, if applicable.</param>
    public void ErrorJson(StructuredLogBuilder builder, Exception? ex = null) =>
        LogWithContext(null, builder, LogLevel.Error, true, ex);

    /// <summary>
    /// Logs a debug message with structured context as JSON. Meant for areas not with an associated user.
    /// </summary>
    /// <param name="builder">The structured log builder to generate the message from.</param>
    public void DebugJson(StructuredLogBuilder builder) =>
        LogWithContext(null, builder, LogLevel.Debug, true);

    /// <summary>
    /// Logs an informational message with structured context. Meant for quick logging.
    /// </summary>
    /// <param name="action">The action to specify in the log.</param>
    /// <param name="target">The target to specify in the log.</param>
    /// <param name="user">The user making the request, or null for system logs.</param>
    public void InfoSimple(string action, string target, ClaimsPrincipal? user = null) =>
        LogWithContext(user, new StructuredLogBuilder().SetAction(action).SetTarget(target).SetStatus("SUCCESS"), LogLevel.Information);

    /// <summary>
    /// Logs an informational message with structured context as JSON. Meant for quick logging.
    /// </summary>
    /// <param name="action">The action to specify in the log.</param>
    /// <param name="target">The target to specify in the log.</param>
    /// <param name="user">The user making the request, or null for system logs.</param>
    public void InfoSimpleJson(string action, string target, ClaimsPrincipal? user = null) =>
        LogWithContext(user, new StructuredLogBuilder().SetAction(action).SetTarget(target).SetStatus("SUCCESS"), LogLevel.Information, true);

    /// <summary>
    /// Logs a structured message with optional ClaimsPrincipal context and JSON formatting.
    /// </summary>
    /// <param name="user">The user making the request, or null for system logs.</param>
    /// <param name="builder">The log structure to build.</param>
    /// <param name="level">The log severity.</param>
    /// <param name="asJson">If true, log in JSON format.</param>
    /// <param name="ex">An optional exception to include.</param>
    private void LogWithContext(ClaimsPrincipal? user, StructuredLogBuilder builder, LogLevel level, bool asJson = false,
        Exception? ex = null)
    {
        IDisposable? scope = null;
        if (user is not null)
        {
            scope = BeginUserScope(user);
            builder.SetPerformedBy(user);
        }

        var message = asJson ? builder.ToJson() : builder.Build();
        logger.Log(level, ex, message);

        scope?.Dispose();
    }
}
