namespace Application.Logging;

/// <summary>
/// Represents different categories of log types to be used across the application.
/// </summary>
public static class LogTypes
{
    /// <summary>
    /// All Log Types pertaining to Security.
    /// </summary>
    public static class Security
    {
        /// <summary>
        /// Security-relevant activity, e.g., user changes, access grants, or deletes.
        /// </summary>
        public const string Audit = "AUDIT";
    
        /// <summary>
        /// Authentication events (login, logout, failures).
        /// </summary>
        public const string Auth = "AUTH";
    
        /// <summary>
        /// Resource access events (files, endpoints, APIs).
        /// </summary>
        public const string Access = "ACCESS";
    
        /// <summary>
        /// Privileged access usage (e.g., admin actions, sudo).
        /// </summary>
        public const string Priv = "PRIV";
    
        /// <summary>
        /// Triggered alert-worthy conditions (e.g., suspicious behavior).
        /// </summary>
        public const string Alert = "ALERT";
    
        /// <summary>
        /// Detected threats or indicators of compromise (IoCs).
        /// </summary>
        public const string Threat = "THREAT";
    }

    /// <summary>
    /// All Log Types pertaining to Development or Operations.
    /// </summary>
    public static class DevOps
    {
        /// <summary>
        /// Infrastructure-related events (e.g., service start/stop, errors).
        /// </summary>
        public const string System = "SYSTEM";
    
        /// <summary>
        /// General application lifecycle events.
        /// </summary>
        public const string App = "APP";
    
        /// <summary>
        /// User-initiated activity (updates, submissions, profile edits).
        /// </summary>
        public const string User = "USER";
    
        /// <summary>
        /// Performance logging (e.g., slow queries, response time).
        /// </summary>
        public const string Performance = "PERF";
    
        /// <summary>
        /// Configuration changes, system tuning.
        /// </summary>
        public const string Config = "CONFIG";
    
        /// <summary>
        /// External system/API interactions.
        /// </summary>
        public const string Integration = "INTEGRATION";
    
        /// <summary>
        /// Background job or scheduled task logging.
        /// </summary>
        public const string Job = "JOB";
    }

    public static class Domain
    {
        /// <summary>
        /// Organization types specific events.
        /// </summary>
        public const string Organization = "ORGANIZATION";
    }
    
    
}