namespace Domain.Attributes;

/// <summary>
/// Marks an entity for automatic audit logging.
/// When applied to a class, changes to that entity will be recorded in the audit ledger.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class AuditedAttribute : Attribute
{
    /// <summary>
    /// Optional custom entity type name for the audit log.
    /// If not specified, the class name is used.
    /// </summary>
    public string? EntityTypeName { get; set; }

    /// <summary>
    /// Whether to include the old values in update/delete audits.
    /// Default is true.
    /// </summary>
    public bool IncludeOldValues { get; set; } = true;

    /// <summary>
    /// Whether to include the new values in create/update audits.
    /// Default is true.
    /// </summary>
    public bool IncludeNewValues { get; set; } = true;
}

/// <summary>
/// Marks a property to be excluded from audit logging.
/// Use this for sensitive data that shouldn't appear in audit logs.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class NotAuditedAttribute : Attribute;

/// <summary>
/// Marks a property as containing sensitive data that should be redacted in audit logs.
/// The property name will appear but the value will show as "[REDACTED]".
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class SensitiveDataAttribute : Attribute;