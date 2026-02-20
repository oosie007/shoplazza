using Microsoft.EntityFrameworkCore;
using ShoplazzaAddonApp.Data;
using System.Linq.Expressions;

namespace ShoplazzaAddonApp.Services;

/// <summary>
/// Generic repository implementation for data access operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly ILogger<Repository<T>> _logger;

    public Repository(ApplicationDbContext context, ILogger<Repository<T>> logger)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = logger;
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        try
        {
            return await _dbSet.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> GetAsync(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        string includeProperties = "")
    {
        try
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split(
                new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            if (orderBy != null)
            {
                return await orderBy(query).ToListAsync();
            }

            return await query.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entities of type {EntityType} with filter", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<T?> GetByIdAsync(object id)
    {
        try
        {
            return await _dbSet.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity of type {EntityType} by ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public virtual async Task<T?> GetFirstOrDefaultAsync(
        Expression<Func<T, bool>>? filter = null,
        string includeProperties = "")
    {
        try
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split(
                new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            return await query.FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting first entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> filter)
    {
        try
        {
            return await _dbSet.AnyAsync(filter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of entity type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
    {
        try
        {
            if (filter != null)
            {
                return await _dbSet.CountAsync(filter);
            }

            return await _dbSet.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        try
        {
            await _dbSet.AddAsync(entity);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities)
    {
        try
        {
            await _dbSet.AddRangeAsync(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding range of entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual Task<T> UpdateAsync(T entity)
    {
        try
        {
            _dbSet.Update(entity);
            return Task.FromResult(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual Task UpdateRangeAsync(IEnumerable<T> entities)
    {
        try
        {
            _dbSet.UpdateRange(entities);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating range of entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task DeleteAsync(T entity)
    {
        try
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task DeleteAsync(object id)
    {
        try
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                await DeleteAsync(entity);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity of type {EntityType} by ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public virtual Task DeleteRangeAsync(IEnumerable<T> entities)
    {
        try
        {
            _dbSet.RemoveRange(entities);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting range of entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<int> SaveAsync()
    {
        try
        {
            return await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes to database");
            throw;
        }
    }
}