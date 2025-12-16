using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Domain.Entities.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing WebAuthn credentials.
/// Provides data access methods for FIDO2/WebAuthn credential storage and retrieval.
/// </summary>
public class WebAuthnCredentialRepository : IWebAuthnCredentialRepository
{
    private readonly ICrudOperator<WebAuthnCredential> credentialCrudOperator;

    public WebAuthnCredentialRepository(ICrudOperator<WebAuthnCredential> credentialCrudOperator)
    {
        this.credentialCrudOperator = credentialCrudOperator;
    }

    /// <inheritdoc />
    public async Task<WebAuthnCredential?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await credentialCrudOperator
            .GetAll()
            .Include(w => w.MfaMethod)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WebAuthnCredential?> GetByCredentialIdAsync(string credentialId, CancellationToken cancellationToken = default)
    {
        return await credentialCrudOperator
            .GetAll()
            .Include(w => w.MfaMethod)
            .FirstOrDefaultAsync(w => w.CredentialId == credentialId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WebAuthnCredential>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await credentialCrudOperator
            .GetAll()
            .Include(w => w.MfaMethod)
            .Where(w => w.UserId == userId)
            .Where(w => w.IsActive)
            .OrderByDescending(w => w.LastUsedAt)
            .ThenByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WebAuthnCredential>> GetByMfaMethodIdAsync(Guid mfaMethodId, CancellationToken cancellationToken = default)
    {
        return await credentialCrudOperator
            .GetAll()
            .Include(w => w.MfaMethod)
            .Where(w => w.MfaMethodId == mfaMethodId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetActiveCredentialCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await credentialCrudOperator
            .GetAll()
            .CountAsync(w => w.UserId == userId && w.IsActive, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> CredentialExistsAsync(string credentialId, CancellationToken cancellationToken = default)
    {
        return await credentialCrudOperator
            .GetAll()
            .AnyAsync(w => w.CredentialId == credentialId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WebAuthnCredential>> GetUnusedCredentialsAsync(DateTimeOffset unusedSince, CancellationToken cancellationToken = default)
    {
        return await credentialCrudOperator
            .GetAll()
            .Where(w => w.IsActive)
            .Where(w => w.LastUsedAt == null || w.LastUsedAt < unusedSince)
            .Where(w => w.CreatedAt < unusedSince) // Also check creation date for never-used credentials
            .OrderBy(w => w.LastUsedAt)
            .ThenBy(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default)
    {
        await credentialCrudOperator.AddAsync(credential, cancellationToken);
    }

    /// <inheritdoc />
    public void Remove(WebAuthnCredential credential)
    {
        credentialCrudOperator.Delete(credential);
    }

    /// <inheritdoc />
    public async Task<int> RemoveByMfaMethodIdAsync(Guid mfaMethodId, CancellationToken cancellationToken = default)
    {
        var credentials = await credentialCrudOperator
            .GetAll()
            .Where(w => w.MfaMethodId == mfaMethodId)
            .ToListAsync(cancellationToken);

        foreach (var credential in credentials)
        {
            credentialCrudOperator.Delete(credential);
        }

        return credentials.Count;
    }

    /// <inheritdoc />
    public async Task<int> DeactivateUnusedCredentialsAsync(DateTimeOffset unusedSince, CancellationToken cancellationToken = default)
    {
        var unusedCredentials = await credentialCrudOperator
            .GetAll()
            .Where(w => w.IsActive)
            .Where(w => w.LastUsedAt == null || w.LastUsedAt < unusedSince)
            .Where(w => w.CreatedAt < unusedSince)
            .ToListAsync(cancellationToken);

        foreach (var credential in unusedCredentials)
        {
            credential.Deactivate();
        }

        return unusedCredentials.Count;
    }
}
