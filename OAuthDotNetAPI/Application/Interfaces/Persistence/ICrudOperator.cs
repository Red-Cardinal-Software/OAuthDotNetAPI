namespace Application.Interfaces.Persistence;

/// <summary>
/// Represents a generic interface for performing Create, Read, Update, and Delete (CRUD) operations on entities of type <typeparamref name="TEntity"/>.
/// </summary>
/// <typeparam name="TEntity">The type of the entity on which CRUD operations will be performed. This type must be a class.</typeparam>
public interface ICrudOperator<TEntity> where TEntity : class
{
    /// <summary>
    /// Retrieves all entities of type <typeparamref name="TEntity"/> from the database.
    /// </summary>
    /// <returns>An <see cref="IQueryable{T}"/> representing all entities of type <typeparamref name="TEntity"/> in the database.</returns>
    public IQueryable<TEntity> GetAll();

    /// <summary>
    /// Deletes the specified entity of type <typeparamref name="TEntity"/> from the database.
    /// </summary>
    /// <param name="entity">The entity of type <typeparamref name="TEntity"/> to be deleted.</param>
    void Delete(TEntity entity);

    /// <summary>
    /// Deletes multiple entities of type <typeparamref name="TEntity"/> from the database.
    /// </summary>
    /// <param name="entities">A collection of entities of type <typeparamref name="TEntity"/> to be deleted.</param>
    void DeleteMany(IEnumerable<TEntity> entities);

    /// <summary>
    /// Adds a new entity of type <typeparamref name="TEntity"/> to the database asynchronously.
    /// </summary>
    /// <param name="entity">The entity of type <typeparamref name="TEntity"/> to be added.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation, containing the added entity of type <typeparamref name="TEntity"/>.</returns>
    Task<TEntity> AddAsync(TEntity entity);

    /// <summary>
    /// Adds multiple entities of type <typeparamref name="TEntity"/> to the database asynchronously.
    /// </summary>
    /// <param name="entities">A collection of entities of type <typeparamref name="TEntity"/> to be added.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task AddManyAsync(IEnumerable<TEntity> entities);
}