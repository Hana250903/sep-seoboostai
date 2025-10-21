using Microsoft.EntityFrameworkCore;
using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories
{
    public class UserRepository : GenericRepository<User>
    {
        public UserRepository() { }

        /// <summary>
        /// Retrieves a paginated list of users along with pagination metadata.
        /// </summary>
        /// <param name="currentPage">The current page number to retrieve.</param>
        /// <param name="pageSize">The number of users per page.</param>
        /// <returns>A <see cref="PaginationResult{List{User}}"/> containing the users for the specified page and pagination details.</returns>
        public async Task<PaginationResult<List<User>>> GetUserWithPaginateAsync(int currentPage, int pageSize)
        {
            var users = await GetAllAsync();

            var totalItems = users.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            users = users
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new PaginationResult<List<User>>
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = currentPage,
                PageSize = pageSize,
                Items = users
            };
            return result;
        }

        /// <summary>
        /// Asynchronously retrieves the first user with the specified email address.
        /// </summary>
        /// <param name="email">The email address to search for.</param>
        /// <returns>The user matching the email, or null if no user is found.</returns>
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
