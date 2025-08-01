﻿using EventManager.Data.Entities.Events.Base;
using Microsoft.EntityFrameworkCore;

namespace EventManager.Data.Repositories.Base;

public abstract class GenericRepository<TEntity> where TEntity : class, IEntity
{
    private readonly DbContext _dbContext;

    private List<Guid> deletedIds = new List<Guid>();
    protected DbSet<TEntity> Entities => _dbContext.Set<TEntity>();

    protected GenericRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
        _dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
    }

    protected DbSet<TOtherEntity> FindDbSet<TOtherEntity>() where TOtherEntity : class, IEntity
    {
        return _dbContext.Set<TOtherEntity>();
    }

    public async Task<TEntity> AddAsync(TEntity entity)
    {
        return (await Entities.AddAsync(entity)).Entity;
    }

    public Task UpdateAsync(TEntity entity)
    {
        Entities.Update(entity);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(TEntity entity)
    {
        if (entity is IPersistentEntity persistenEntity)
        {
            persistenEntity.IsDeleted = true;

            return UpdateAsync(entity);
        }

        Entities.Remove(entity);
        deletedIds.Add(entity.Id);

        return Task.CompletedTask;
    }

    public async Task RemoveAsync(Guid id)
    {
        TEntity entity = await Entities.Where(x => x.Id == id).SingleAsync();

        await RemoveAsync(entity);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    public Task<TEntity?> FindByEntityAsync(Guid id)
    {
        return Entities.SingleOrDefaultAsync(x => x.Id == id);
    }

    public Task<List<TEntity>> ListAllAsync()
    {
        return Entities.ToListAsync();
    }

    protected async Task<bool> IsDeletedAsync(Guid entityId)
    {
        if (typeof(TEntity) == typeof(IPersistentEntity))
        {
            return await Entities.Where(x => x.Id == entityId).AnyAsync(x => ((IPersistentEntity)x).IsDeleted);
        }

        TEntity? entity = await Entities.SingleOrDefaultAsync(x => x.Id == entityId);

        if (entity == null)
        {
            return true;
        }

        return deletedIds.Contains(entityId);
    }

    protected bool HasChanges(TEntity entity)
    {
        _dbContext.Entry(entity).DetectChanges();
        return _dbContext.ChangeTracker.Entries().Any(z => z.Entity == entity && z.State == EntityState.Modified);
    }
}
