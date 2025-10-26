using Microsoft.EntityFrameworkCore.Storage;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork, IDisposable, IAsyncDisposable
    {
        private readonly SEP_SEOBoostAIContext _dbContext;
        private IDbContextTransaction? _transaction = null;

        public UnitOfWork(SEP_SEOBoostAIContext SEP_SEOBoostAIContext)
        {
            _dbContext = SEP_SEOBoostAIContext;
        }
        public async Task BeginTransactionAsync()
        {
            // Chỉ bắt đầu transaction mới nếu chưa có
            if (_transaction == null)
            {
                _transaction = await _dbContext.Database.BeginTransactionAsync();
            }
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null; // <-- CẬP NHẬT 3: Gán về null
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null; // <-- CẬP NHẬT 3: Gán về null
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // CẬP NHẬT 2: Implement IAsyncDisposable
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(false); // Chỉ dọn dẹp tài nguyên unmanaged (nếu có)
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Giải phóng tài nguyên đồng bộ (nếu có)
                _transaction?.Dispose(); // Hủy transaction nếu còn
                _dbContext.Dispose();    // Hủy context
            }
            _transaction = null;
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            // Giải phóng tài nguyên bất đồng bộ
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }
            _transaction = null;

            await _dbContext.DisposeAsync();
        }
    }
}
