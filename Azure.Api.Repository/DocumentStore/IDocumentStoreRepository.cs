using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Azure.Api.Repository
{
    public interface IDocumentStoreRepository<T>
    {
        Task<T> GetByIdAsync(params object[] keyValues);
        Task<T> GetByIdAsync(int id);
        IQueryable<T> Query();
        IQueryable<T> List(Expression<Func<T, bool>> predicate);
        Task<T> Add(T entity);
        Task<bool> Delete(T entity);
        Task<bool> Update(T entity);
        Task<bool> UpdateRange(IEnumerable<T> entities);
        Task<T> GetByAsync(Expression<Func<T, bool>> predicate);
        Task<bool> AddRange(IEnumerable<T> entities);
        Task<bool> DeleteRange(IEnumerable<T> entities);
        void Attach(T obj);
    }
}
