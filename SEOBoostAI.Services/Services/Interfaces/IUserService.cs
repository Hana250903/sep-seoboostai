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
        Task<List<User>> GetUsersAsync();
        Task<PaginationResult<List<User>>> GetUsersWithPaginateAsync(int currentPage, int pageSize, string? role, bool? isBanned, bool? isDeleted);
        Task<User> GetUserByIdAsync(int id);
        Task CreateAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(int id);
        Task<User> UpdateUserToStaff(int userId);
    }
}
