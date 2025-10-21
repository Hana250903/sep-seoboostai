using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
    public class UserService : IUserService
    {
        private readonly UserRepository _userRepository;
        public UserService() => _userRepository ??= new UserRepository();

        public async Task<int> CreateAsync(User user)
        {
            return await _userRepository.CreateAsync(user);
        }

        public async Task<List<User>> GetUsersAsync()
        {
            return await _userRepository.GetAllAsync();
        }

        public async Task<int> UpdateAsync(User user)
        {
            return await _userRepository.UpdateAsync(user);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return await _userRepository.RemoveAsync(user);
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task<PaginationResult<List<User>>> GetUsersWithPaginateAsync(int currentPage, int pageSize)
        {
            return await _userRepository.GetUserWithPaginateAsync(currentPage, pageSize);
        }
    }
}
