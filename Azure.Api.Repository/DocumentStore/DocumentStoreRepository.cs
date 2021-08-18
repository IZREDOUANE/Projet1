using Azure.Api.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Azure.Api.Repository
{
    public class DocumentStoreRepository<TEntity> : IDocumentStoreRepository<TEntity> where TEntity : class
    {
        protected readonly DocumentStoreContext _dbContext;

        public DocumentStoreRepository(DocumentStoreContext dbContext)
        {
            _dbContext = dbContext;
        }

        ~DocumentStoreRepository()
        {
            _dbContext?.Dispose();
        }

        public async Task<TEntity> Add(TEntity entity)
        {
            try
            {
                await _dbContext.Set<TEntity>().AddAsync(entity);
                await _dbContext.SaveChangesAsync();
                return entity;
            }

            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> AddRange(IEnumerable<TEntity> entities)
        {
            try
            {
                _dbContext.Set<TEntity>().AddRange(entities);
                await _dbContext.SaveChangesAsync();

                return true;
            }

            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> Delete(TEntity entity)
        {
            try
            {
                _dbContext.Set<TEntity>().Remove(entity);
                await _dbContext.SaveChangesAsync();

                return true;
            }
            
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteRange(IEnumerable<TEntity> entities)
        {
            try
            {
                _dbContext.Set<TEntity>().RemoveRange(entities);
                await _dbContext.SaveChangesAsync();

                return true;
            }

            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> Update(TEntity entity)
        {
            try
            {
                _dbContext.Entry(entity).State = EntityState.Modified;
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateRange(IEnumerable<TEntity> entities)
        {
            try
            {
                _dbContext.Set<TEntity>().UpdateRange(entities);
                await _dbContext.SaveChangesAsync();

                return true;
            }

            catch (Exception)
            {
                return false;
            }
        }

        public async Task<TEntity> GetByIdAsync(int id)
        {
            return await _dbContext.Set<TEntity>().FindAsync(id);
        }

        public async Task<TEntity> GetByIdAsync(params object[] keyValues)
        {
            return await _dbContext.Set<TEntity>().FindAsync(keyValues);
        }

        public IQueryable<TEntity> Query()
        {
            return _dbContext.Set<TEntity>();
        }

        public void Attach(TEntity obj)
        {
            _dbContext.Attach(obj);
        }

        public IQueryable<TEntity> List(Expression<Func<TEntity, bool>> predicate)
        {
            return _dbContext.Set<TEntity>().Where(predicate);
        }

        public async Task<TEntity> GetByAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbContext.Set<TEntity>().FirstOrDefaultAsync(predicate);
        }
    }
}
