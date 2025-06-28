namespace Application.Interfaces.Persistence;

/// <summary>
/// Defines a contract for managing database transactions and coordinating changes
/// to the underlying data store in an atomic manner.
/// </summary>
/// <remarks>
/// This interface is responsible for providing a single method to commit changes
/// to the database, ensuring consistency and atomicity. It is commonly used in
/// conjunction with the repository pattern to manage the lifecycle of transactions.
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all changes made in the current database context.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that can be used to observe cancellation requests for the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the number
    /// of state entries written to the underlying database.
    /// </returns>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}