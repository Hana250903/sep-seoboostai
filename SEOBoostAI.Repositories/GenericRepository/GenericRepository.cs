using Microsoft.EntityFrameworkCore;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.GenericRepository
{
    public class GenericRepository<T> where T : class
    {
        protected readonly SEP_SEOBoostAIContext _context;


        // Bắt buộc dùng constructor này để đảm bảo Dependency Injection
        // và tất cả repository dùng chung 1 DbContext
        public GenericRepository(SEP_SEOBoostAIContext context)
        {
            _context = context;
        }

        public List<T> GetAll()
        {
            return _context.Set<T>().ToList();
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }

        // --- Create ---
        public void Create(T entity)
        {
            _context.Set<T>().Add(entity);
        }

        public async Task CreateAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
        }

        // --- Update ---
        public void Update(T entity)
        {
            _context.Set<T>().Update(entity);
        }

        public Task UpdateAsync(T entity)
        {
            _context.Set<T>().Update(entity);
            return Task.CompletedTask;
        }

        // --- Remove (Hard Delete) ---
        public void Remove(T entity)
        {
            _context.Set<T>().Remove(entity);
        }

        public Task RemoveAsync(T entity)
        {
            _context.Set<T>().Remove(entity);
            return Task.CompletedTask;
        }

        // --- GetById ---
        public T GetById(int id)
        {
            return _context.Set<T>().Find(id);
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public T GetById(string code)
        {
            return _context.Set<T>().Find(code);
        }

        public async Task<T> GetByIdAsync(string code)
        {
            return await _context.Set<T>().FindAsync(code);
        }

        public T GetById(Guid code)
        {
            return _context.Set<T>().Find(code);
        }

        public async Task<T> GetByIdAsync(Guid code)
        {
            return await _context.Set<T>().FindAsync(code);
        }

        #region Range Operations

        public void CreateRange(IEnumerable<T> entities)
        {
            _context.Set<T>().AddRange(entities);
        }

        public async Task CreateRangeAsync(IEnumerable<T> entities)
        {
            await _context.Set<T>().AddRangeAsync(entities);
        }

        public void UpdateRange(IEnumerable<T> entities)
        {
            _context.Set<T>().UpdateRange(entities);
        }

        public Task UpdateRangeAsync(IEnumerable<T> entities)
        {
            _context.Set<T>().UpdateRange(entities);
            return Task.CompletedTask;
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _context.Set<T>().RemoveRange(entities);
        }

        public Task RemoveRangeAsync(IEnumerable<T> entities)
        {
            _context.Set<T>().RemoveRange(entities);
            return Task.CompletedTask;
        }

        #endregion

        #region Soft Delete (Giả định "ShortDelete" là "SoftDelete")

        /// <summary>
        /// Thực hiện "Soft Delete" (Xóa mềm).
        /// Yêu cầu entity <T> phải có thuộc tính "IsDeleted" kiểu bool.
        /// </summary>
        public void ShortDelete(T entity)
        {
            var entityEntry = _context.Entry(entity);

            // Tìm thuộc tính "IsDeleted"
            var property = entityEntry.Property("IsDeleted");

            if (property == null)
            {
                throw new InvalidOperationException($"Entity {typeof(T).Name} không có thuộc tính 'IsDeleted' để thực hiện ShortDelete (Soft Delete).");
            }

            // Set IsDeleted = true và đánh dấu entity đã bị thay đổi
            property.CurrentValue = true;
            entityEntry.State = EntityState.Modified;
        }

        /// <summary>
        /// Thực hiện "Soft Delete" (Xóa mềm) bất đồng bộ.
        /// Yêu cầu entity <T> phải có thuộc tính "IsDeleted" kiểu bool.
        /// </summary>
        public Task ShortDeleteAsync(T entity)
        {
            ShortDelete(entity); // Logic không phải I/O, chạy đồng bộ
            return Task.CompletedTask;
        }

        /// <summary>
        /// Thực hiện "Soft Delete" (Xóa mềm) cho một danh sách entities.
        /// Yêu cầu entity <T> phải có thuộc tính "IsDeleted" kiểu bool.
        /// </summary>
        public void ShortDeleteRange(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                ShortDelete(entity);
            }
        }

        /// <summary>
        /// Thực hiện "Soft Delete" (Xóa mềm) bất đồng bộ cho một danh sách entities.
        /// Yêu cầu entity <T> phải có thuộc tính "IsDeleted" kiểu bool.
        /// </summary>
        public Task ShortDeleteRangeAsync(IEnumerable<T> entities)
        {
            ShortDeleteRange(entities);
            return Task.CompletedTask;
        }

        #endregion


        public async Task<T> GetAsync(
    Expression<Func<T, bool>> filter = null,
    string includeProperties = "")
        {
            IQueryable<T> query = _context.Set<T>().AsQueryable();

            // 1. Áp dụng bộ lọc (WHERE)
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // 2. Áp dụng Include (JOIN các bảng con)
            if (!string.IsNullOrEmpty(includeProperties))
            {
                // Tách chuỗi "InterestOverTimes,RelatedTopics,..."
                foreach (var includeProperty in includeProperties.Split(
                    new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
            }

            // 3. Trả về đối tượng đầu tiên tìm thấy
            return await query.FirstOrDefaultAsync();
        }

    }
}
