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

        // =========================
        // Get All
        // =========================
        public async Task<IEnumerable<TEntity>> GetAllAsync(bool trackChanges = false)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();

            return trackChanges
                ? await query.ToListAsync()
                : await query.AsNoTracking().ToListAsync();
        }

        // =========================
        // Get All IQueryable
        // =========================
        public IQueryable<TEntity> GetAllQueryable(bool trackChanges = false)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();
            return trackChanges ? query : query.AsNoTracking();
        }

        // =========================
        // Get By Id
        // =========================
        public async Task<TEntity?> GetByIdAsync(TKey id)
        {
            return await _context.Set<TEntity>().FindAsync(id);
        }

        // =========================
        // Add
        // =========================
        public async Task AddAsync(TEntity entity)
        {
            await _context.Set<TEntity>().AddAsync(entity);
        }

        // =========================
        // Update
        // =========================
        public void Update(TEntity entity)
        {
            _context.Set<TEntity>().Update(entity);
        }

        // =========================
        // Delete
        // =========================
        public void Delete(TEntity entity)
        {
            _context.Set<TEntity>().Remove(entity);
        }

        // =========================
        // Add Range
        // =========================
        public async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            await _context.Set<TEntity>().AddRangeAsync(entities);
        }

        // =========================
        // Delete Range
        // =========================
        public void DeleteRange(IEnumerable<TEntity> entities)
        {
            _context.Set<TEntity>().RemoveRange(entities);
        }

        // =========================
        // Find With Predicate
        // =========================
        public IQueryable<TEntity> FindByCondition(
            Expression<Func<TEntity, bool>> expression,
            bool trackChanges = false)
        {
            return trackChanges
                ? _context.Set<TEntity>().Where(expression)
                : _context.Set<TEntity>().AsNoTracking().Where(expression);
        }

        // =========================
        // First Or Default With Include
        // =========================
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
