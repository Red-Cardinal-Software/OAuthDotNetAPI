using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Domain.Entities.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MfaPushRepository(ICrudOperator<MfaPushDevice> pushDeviceCrud, ICrudOperator<MfaPushChallenge> pushChallengeCrud) : IMfaPushRepository
{
    public Task<MfaPushDevice?> GetPushDeviceAsync(Guid id, CancellationToken cancellationToken = default) =>
        pushDeviceCrud.GetAll()
            .Include(x => x.MfaMethod)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);

    public Task<MfaPushDevice?> GetPushDeviceByDeviceIdAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default) =>
        pushDeviceCrud.GetAll().Include(x => x.MfaMethod)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceId == deviceId, cancellationToken);

    public async Task<IReadOnlyList<MfaPushDevice>> GetUserPushDevicesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var devices = await pushDeviceCrud.GetAll().Include(x => x.MfaMethod)
            .Where(x => x.UserId == userId).ToListAsync(cancellationToken);

        return devices.AsReadOnly();
    }

    public async Task AddPushDeviceAsync(MfaPushDevice device, CancellationToken cancellationToken = default) =>
        await pushDeviceCrud.AddAsync(device, cancellationToken);

    public Task<MfaPushChallenge?> GetPushChallengeAsync(Guid id, CancellationToken cancellationToken = default) =>
        pushChallengeCrud.GetAll()
            .Include(x => x.Device)
            .Include(x => x.Status)
            .Include(x => x.Response)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task AddPushChallengeAsync(MfaPushChallenge challenge, CancellationToken cancellationToken = default) =>
        pushChallengeCrud.AddAsync(challenge, cancellationToken);

    public Task<int> GetRecentPushChallengesCountAsync(Guid userId, TimeSpan window, CancellationToken cancellationToken = default) =>
        pushChallengeCrud.GetAll()
            .Where(x => x.UserId == userId && x.CreatedAt < DateTime.UtcNow.Subtract(window))
            .CountAsync(cancellationToken);

    public async Task<int> DeleteExpiredPushChallengesAsync(DateTime cutoff, CancellationToken cancellationToken = default)
    {
        var expiredPushChallenges = await pushChallengeCrud.GetAll().Where(x => x.ExpiresAt < cutoff).ToListAsync(cancellationToken);
        pushChallengeCrud.DeleteMany(expiredPushChallenges);
        return expiredPushChallenges.Count;
    }
}
