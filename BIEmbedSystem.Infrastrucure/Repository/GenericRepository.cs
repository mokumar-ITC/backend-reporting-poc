using BIEmbedSystem.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Infrastrucure.Repository
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected MDMDbContext _context;
        protected DbSet<T> dbSet;
        protected readonly ILogger _logger;
        public GenericRepository(
            MDMDbContext context,
            ILogger logger)
        {
            _context = context;
            _logger = logger;
            dbSet = _context.Set<T>();
        }

        public async Task<bool> Add(T entity)
        {
            await dbSet.AddAsync(entity);
            return true;
        }
        public bool Update(T entity)
        {
            try
            {
                var keyName = _context.Model.FindEntityType(typeof(T))
                    ?.FindPrimaryKey()
                    ?.Properties
                    ?.Select(x => x.Name)
                    ?.FirstOrDefault();

                if (keyName != null)
                {
                    var keyValue = _context.Entry(entity).Property(keyName).CurrentValue;

                    // find if entity with same key already tracked
                    var localEntity = dbSet.Local
                        .FirstOrDefault(e => _context.Entry(e).Property(keyName).CurrentValue.Equals(keyValue));

                    if (localEntity != null)
                    {
                        // Detach the already tracked instance
                        _context.Entry(localEntity).State = EntityState.Detached;
                    }
                }

                dbSet.Update(entity); // safely attach & mark as modified
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating entity {typeof(T).Name}");
                return false;
            }
        }


        public async Task<bool> AddRangeAsync(IEnumerable<T> entities)
        {
            await dbSet.AddRangeAsync(entities);
            return true;
        }
        public IEnumerable<T> Find(Expression<Func<T, bool>> expression)
        {
            return dbSet.Where(expression);
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            return await dbSet.ToListAsync();
        }
       
        public async Task<T?> GetById(int id)
        {
            return await dbSet.FindAsync(id);
        }


        public void Remove(T entity)
        {
            dbSet.Remove(entity);
        }

        public void Upsert(T entity)
        {
            dbSet.Update(entity);

        }
        public async Task<T?> GetByIds(string Id,string col)
        {
            return await dbSet.FirstOrDefaultAsync(e => EF.Property<string>(e, col) == Id);
        }
        
    }

}
