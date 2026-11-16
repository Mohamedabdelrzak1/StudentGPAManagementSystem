using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Domain.Contracts;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;

namespace Persistence.Repositorys
{
    public class GenericRepository<TEntity, TKey>
          : IGenericRepository<TEntity, TKey>
          where TEntity : BaseEntity<TKey>
    {
        private readonly StudentGpaDbContext _context;

        public GenericRepository(StudentGpaDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync(bool trackChanges = false)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();

            return trackChanges
                ? await query.ToListAsync()
                : await query.AsNoTracking().ToListAsync();
        }

        public IQueryable<TEntity> GetAllQueryable(bool trackChanges = false)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();
            return trackChanges ? query : query.AsNoTracking();
        }

        public async Task<TEntity?> GetByIdAsync(TKey id)
        {
            return await _context.Set<TEntity>().FindAsync(id);
        }

        public async Task AddAsync(TEntity entity)
        {
            await _context.Set<TEntity>().AddAsync(entity);
        }

        public void Update(TEntity entity)
        {
            var tracked = _context.Set<TEntity>().Local.FirstOrDefault(e => Equals(e.Id, entity.Id));
            if (tracked != null)
            {
                if (!ReferenceEquals(tracked, entity))
                {
                    _context.Entry(tracked).CurrentValues.SetValues(entity);
                }
                _context.Entry(tracked).State = EntityState.Modified;
                return;
            }

            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                _context.Set<TEntity>().Update(entity);
            }
            else
            {
                entry.State = EntityState.Modified;
            }
        }

        public void Delete(TEntity entity)
        {
            _context.Set<TEntity>().Remove(entity);
        }

        public async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            await _context.Set<TEntity>().AddRangeAsync(entities);
        }

        public void DeleteRange(IEnumerable<TEntity> entities)
        {
            _context.Set<TEntity>().RemoveRange(entities);
        }

        public IQueryable<TEntity> FindByCondition(
            Expression<Func<TEntity, bool>> expression,
            bool trackChanges = false)
        {
            return trackChanges
                ? _context.Set<TEntity>().Where(expression)
                : _context.Set<TEntity>().AsNoTracking().Where(expression);
        }

        public async Task<TEntity?> GetFirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            bool trackChanges = false)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();

            if (include != null)
                query = include(query);

            if (!trackChanges)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(predicate);
        }
    }
}
