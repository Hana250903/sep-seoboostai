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
    public class PerformanceService : IPerformanceService
    {
        private readonly PerformanceRepository _performanceRepository;
        private readonly UserRepository _userRepository;

        public PerformanceService(PerformanceRepository performanceRepository, UserRepository userRepository)
        {
            _performanceRepository = performanceRepository;
            _userRepository = userRepository;
        }

        public async Task<int> CreateAsync(Performance performance)
        {
            return await _performanceRepository.CreateAsync(performance);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var performance = await _performanceRepository.GetByIdAsync(id);
            return await _performanceRepository.RemoveAsync(performance);
        }

        public async Task<Performance> GetPerformanceByIdAsync(int id)
        {
            return await _performanceRepository.GetByIdAsync(id);
        }

        public async Task<List<Performance>> GetPerformancesAsync()
        {
            return await _performanceRepository.GetAllAsync();
        }

        public async Task<PaginationResult<List<Performance>>> GetPerformancesWithPaginateAsync(int currentPage, int pageSize)
        {
            return await _performanceRepository.GetPerformancesWithPaginateAsync(currentPage, pageSize);
        }

        public async Task<int> UpdateAsync(Performance performance)
        {
            return await _performanceRepository.UpdateAsync(performance);
        }
    }
}
