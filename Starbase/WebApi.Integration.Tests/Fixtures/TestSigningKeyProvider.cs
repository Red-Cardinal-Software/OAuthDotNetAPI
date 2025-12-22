using System.Security.Cryptography;
using Application.Interfaces.Providers;
using Microsoft.IdentityModel.Tokens;

namespace WebApi.Integration.Tests.Fixtures;

/// <summary>
/// Test signing key provider that allows simulating key rotation scenarios.
/// </summary>
public class TestSigningKeyProvider : ISigningKeyProvider
{
    private readonly List<SigningKeyInfo> _keys = new();
    private readonly object _lock = new();

    public TestSigningKeyProvider()
    {
        // Initialize with a primary key
        var primaryKey = GenerateKey("primary-key", isPrimary: true);
        _keys.Add(primaryKey);
    }

    /// <summary>
    /// Gets the current primary signing key.
    /// </summary>
    public Task<SigningKeyInfo> GetCurrentSigningKeyAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var primary = _keys.FirstOrDefault(k => k.IsPrimary)
                ?? throw new InvalidOperationException("No primary key found");
            return Task.FromResult(primary);
        }
    }

    /// <summary>
    /// Gets all valid keys for token validation (primary + previous keys within overlap window).
    /// </summary>
    public Task<IReadOnlyList<SigningKeyInfo>> GetValidationKeysAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            var validKeys = _keys
                .Where(k => k.ExpiresAt == null || k.ExpiresAt > now)
                .ToList();
            return Task.FromResult<IReadOnlyList<SigningKeyInfo>>(validKeys);
        }
    }

    /// <summary>
    /// Simulates key rotation - creates a new primary key and demotes the old one.
    /// </summary>
    public Task<SigningKeyInfo> RotateKeyAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            // Demote current primary - materialize the list first to avoid modification during enumeration
            var primaryKeys = _keys.Where(k => k.IsPrimary).ToList();
            foreach (var key in primaryKeys)
            {
                // Create a new instance with IsPrimary = false
                var index = _keys.IndexOf(key);
                _keys[index] = new SigningKeyInfo
                {
                    KeyId = key.KeyId,
                    Key = key.Key,
                    CreatedAt = key.CreatedAt,
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(7), // Overlap window
                    IsPrimary = false
                };
            }

            // Create new primary key
            var newKey = GenerateKey($"rotated-key-{DateTimeOffset.UtcNow.Ticks}", isPrimary: true);
            _keys.Insert(0, newKey);

            return Task.FromResult(newKey);
        }
    }

    public Task<bool> IsRotationDueAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task RefreshKeysAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds an expired key for testing rejection of expired keys.
    /// </summary>
    public void AddExpiredKey(string keyId)
    {
        lock (_lock)
        {
            var expiredKey = new SigningKeyInfo
            {
                KeyId = keyId,
                Key = GenerateSecurityKey(),
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1), // Already expired
                IsPrimary = false
            };
            _keys.Add(expiredKey);
        }
    }

    /// <summary>
    /// Gets a specific key by ID (for test assertions).
    /// </summary>
    public SigningKeyInfo? GetKeyById(string keyId)
    {
        lock (_lock)
        {
            return _keys.FirstOrDefault(k => k.KeyId == keyId);
        }
    }

    /// <summary>
    /// Gets all keys including expired ones (for test assertions).
    /// </summary>
    public IReadOnlyList<SigningKeyInfo> GetAllKeys()
    {
        lock (_lock)
        {
            return _keys.ToList();
        }
    }

    private static SigningKeyInfo GenerateKey(string keyId, bool isPrimary)
    {
        return new SigningKeyInfo
        {
            KeyId = keyId,
            Key = GenerateSecurityKey(),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = null,
            IsPrimary = isPrimary
        };
    }

    private static SymmetricSecurityKey GenerateSecurityKey()
    {
        var keyBytes = new byte[64];
        RandomNumberGenerator.Fill(keyBytes);
        return new SymmetricSecurityKey(keyBytes);
    }
}