using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.GenericRepository
{
    public interface IGenericRepository<T> where T : class
    {
        Task<List<T>> GetAllAsync();
        Task<T> GetByIdAsync(int id);
        Task<T> GetByIdAsync(string id);
        Task<T> GetByIdAsync(Guid code);

        Task CreateAsync(T entity);
        Task UpdateAsync(T entity);
        Task RemoveAsync(T entity); // Hard delete
        Task ShortDeleteAsync(T entity); // Soft delete

        Task CreateRangeAsync(IEnumerable<T> entities);
        Task UpdateRangeAsync(IEnumerable<T> entities);
        Task RemoveRangeAsync(IEnumerable<T> entities);
        Task ShortDeleteRangeAsync(IEnumerable<T> entities);

        //test gia, check nguoc case
        Task<T> GetAsync(
            Expression<Func<T, bool>> filter = null,
            string includeProperties = "");

    }
}
