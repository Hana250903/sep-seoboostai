using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<User>> GetAllUserAsync();
        Task<PaginationResult<List<User>>> GetUserWithPaginateAsync(int currentPage, int pageSize);
        Task<User> GetUsersByIdAsync(int id);
        Task<int> CreateUserAsync(User user);
        Task<int> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int id);
    }
}
