using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Domain.Entities.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing MFA email codes.
/// Provides data access methods for email-based MFA verification codes.
/// </summary>
public class MfaEmailCodeRepository(ICrudOperator<MfaEmailCode> emailCodeCrudOperator) : IMfaEmailCodeRepository
{
    /// <inheritdoc />
    public async Task<MfaEmailCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await emailCodeCrudOperator
            .GetAll()
            .Include(e => e.Challenge)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MfaEmailCode?> GetValidCodeByChallengeIdAsync(Guid challengeId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await emailCodeCrudOperator
            .GetAll()
            .Include(e => e.Challenge)
            .Where(e => e.MfaChallengeId == challengeId)
            .Where(e => !e.IsUsed)
            .Where(e => e.ExpiresAt > now)
            .Where(e => e.AttemptCount < 3) // Max attempts from entity constant
            .OrderByDescending(e => e.SentAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MfaEmailCode>> GetRecentCodesByUserIdAsync(
        Guid userId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        return await emailCodeCrudOperator
            .GetAll()
            .Where(e => e.UserId == userId)
            .Where(e => e.SentAt >= since)
            .OrderByDescending(e => e.SentAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetCodeCountSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        return await emailCodeCrudOperator
            .GetAll()
            .CountAsync(e => e.UserId == userId && e.SentAt >= since, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(MfaEmailCode emailCode, CancellationToken cancellationToken = default)
    {
        await emailCodeCrudOperator.AddAsync(emailCode, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MfaEmailCode>> GetExpiredCodesAsync(
        DateTimeOffset expiredBefore,
        CancellationToken cancellationToken = default)
    {
        return await emailCodeCrudOperator
            .GetAll()
            .Where(e => e.ExpiresAt < expiredBefore)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeleteExpiredCodesAsync(DateTimeOffset expiredBefore, CancellationToken cancellationToken = default)
    {
        var expiredCodes = await emailCodeCrudOperator
            .GetAll()
            .Where(e => e.ExpiresAt < expiredBefore)
            .ToListAsync(cancellationToken);

        foreach (var code in expiredCodes)
        {
            emailCodeCrudOperator.Delete(code);
        }

        return expiredCodes.Count;
    }
}
