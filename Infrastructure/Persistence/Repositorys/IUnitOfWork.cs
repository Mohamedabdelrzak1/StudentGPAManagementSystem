using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Domain.Contracts;
using Domain.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Persistence.Data;

namespace Persistence.Repositorys
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly StudentGpaDbContext _dbContext;
        private readonly ConcurrentDictionary<string, object> _repositories = new();
        private IDbContextTransaction? _transaction;

        public UnitOfWork(StudentGpaDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
            where TEntity : BaseEntity<TKey>
        {
            var typeName = typeof(TEntity).FullName ?? typeof(TEntity).Name;

            return (IGenericRepository<TEntity, TKey>)_repositories.GetOrAdd(typeName, _ =>
                new GenericRepository<TEntity, TKey>(_dbContext));
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }

        public void ClearTracking()
        {
            _dbContext.ChangeTracker.Clear();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _dbContext.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
            await _dbContext.DisposeAsync();
        }
    }
}
