using System.Text.Json.Serialization;

namespace Infrastructure.Security.SigningKey;

/// <summary>
/// Represents a set of signing keys stored in a secrets manager.
/// Serialized as JSON for storage in cloud secrets managers.
/// </summary>
internal sealed class SigningKeySet
{
    /// <summary>
    /// List of signing key entries, ordered by creation time (newest first).
    /// </summary>
    [JsonPropertyName("keys")]
    public List<SigningKeyEntry> Keys { get; set; } = new();
}

/// <summary>
/// A single signing key entry stored in the key set.
/// </summary>
internal sealed class SigningKeyEntry
{
    /// <summary>
    /// Unique identifier for this key.
    /// </summary>
    [JsonPropertyName("keyId")]
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// The actual key material, base64 encoded.
    /// </summary>
    [JsonPropertyName("keyMaterial")]
    public string KeyMaterial { get; set; } = string.Empty;

    /// <summary>
    /// When this key was created (ISO 8601).
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When this key expires (ISO 8601). Null if it doesn't expire.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Whether this is the primary key for signing new tokens.
    /// </summary>
    [JsonPropertyName("isPrimary")]
    public bool IsPrimary { get; set; }
}