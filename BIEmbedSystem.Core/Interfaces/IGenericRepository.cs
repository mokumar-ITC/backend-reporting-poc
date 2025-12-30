using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Core.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetById(int id);
        Task<T?> GetByIds(string id,string col);
        Task<bool> AddRangeAsync(IEnumerable<T> entities);
        Task<IEnumerable<T>> GetAll();
        IEnumerable<T> Find(Expression<Func<T, bool>> expression);
        Task<bool> Add(T entity);
        //Task<bool> Update(T entity);
        bool Update(T entity);
        void Remove(T entity);
        void Upsert(T entity);

    }
}
