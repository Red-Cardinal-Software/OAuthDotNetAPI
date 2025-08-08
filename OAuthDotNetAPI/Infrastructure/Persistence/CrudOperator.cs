using Application.Interfaces.Persistence;

namespace Infrastructure.Persistence;

public class CrudOperator<TEntity>(AppDbContext context) : ICrudOperator<TEntity> where TEntity : class
{
    public IQueryable<TEntity> GetAll() => context.Set<TEntity>();

    public void Delete(TEntity entity)
    {
        context.Set<TEntity>().Remove(entity);
    }

    public void DeleteMany(IEnumerable<TEntity> entities)
    {
        context.Set<TEntity>().RemoveRange(entities);
    }

    public async Task<TEntity> AddAsync(TEntity entity)
    {
         var savedEntity = await context.Set<TEntity>().AddAsync(entity);
         return savedEntity.Entity;
    }

    public Task AddManyAsync(IEnumerable<TEntity> entities) => context.Set<TEntity>().AddRangeAsync(entities);
}
