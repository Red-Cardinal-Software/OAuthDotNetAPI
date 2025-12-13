using System.Security.Claims;
using System.Text.Json;
using Application.Common.Utilities;

namespace Application.Logging;

/// <summary>
/// Provides a fluent interface for constructing structured log messages with various details like type, action, status, target, entity, and performer.
/// </summary>
public class StructuredLogBuilder
{
    private string _type = LogTypes.Security.Audit;
    private string _action = "UNKNOWN";
    private string _status = "UNKNOWN";
    private string _target = "NONE";
    private string _entity = "UNKNOWN";
    private string _performedBy = "SYSTEM";
    private string _detail = "";

    public StructuredLogBuilder SetType(string type)
    {
        _type = type;
        return this;
    }

    public StructuredLogBuilder SetAction(string action)
    {
        _action = action;
        return this;
    }

    public StructuredLogBuilder SetStatus(string status)
    {
        _status = status;
        return this;
    }

    public StructuredLogBuilder SetTarget(string target)
    {
        _target = target;
        return this;
    }

    public StructuredLogBuilder SetPerformedBy(string performedBy)
    {
        _performedBy = performedBy;
        return this;
    }

    public StructuredLogBuilder SetPerformedBy(ClaimsPrincipal user)
    {
        _performedBy = $"UserId:{RoleUtility.GetUserIdFromClaims(user)},OrgId:{RoleUtility.GetOrgIdFromClaims(user)}";
        return this;
    }

    public StructuredLogBuilder SetDetail(string detail)
    {
        _detail = detail;
        return this;
    }

    public StructuredLogBuilder SetEntity(string entity)
    {
        _entity = entity;
        return this;
    }

    /// <summary>
    /// Builds the general log format with pipe characters to seperate details.
    /// </summary>
    /// <returns>Single-line string version of the log</returns>
    public string Build()
    {
        // Detail is optional
        var detail = string.IsNullOrWhiteSpace(_detail) ? string.Empty : $" | Detail: {_detail}";
        return $"{_type} | Action: {_action} | Status: {_status} | Target: {_target} | Entity: {_entity} | PerformedBy: {_performedBy}{detail}";
    }

    /// <summary>
    /// Support for JSON logging for SIEM's that can use it.
    /// </summary>
    /// <returns>The JSON string to be inserted into the log</returns>
    public string ToJson()
    {
        var logObject = new
        {
            Type = _type,
            Action = _action,
            Status = _status,
            Target = _target,
            Entity = _entity,
            PerformedBy = _performedBy,
            Detail = string.IsNullOrWhiteSpace(_detail) ? null : _detail,
            Timestamp = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(logObject, new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }
}
