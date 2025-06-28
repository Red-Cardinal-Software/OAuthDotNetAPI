using Application.Interfaces.Persistence;

namespace Infrastructure.Persistence;

/// <summary>
/// The UnitOfWork class provides a unified mechanism for managing
/// database transactions and saving changes to the underlying data store
/// in an atomic manner. It coordinates work between multiple repositories
/// to maintain consistency and manage the application's database context.
/// </summary>
/// <remarks>
/// Implements the IUnitOfWork interface to enforce a standard contract
/// for Commit operation across the application infrastructure.
/// </remarks>
public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> CommitAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
}