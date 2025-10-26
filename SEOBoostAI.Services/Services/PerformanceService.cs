using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
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
        private readonly IPerformanceRepository _performanceRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PerformanceService(IPerformanceRepository performanceRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
        {
            _performanceRepository = performanceRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task CreateAsync(Performance performance)
        {
            try
            {
                await _performanceRepository.CreateAsync(performance);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }            
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var performance = await _performanceRepository.GetByIdAsync(id);
                await _performanceRepository.RemoveAsync(performance);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
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

        public async Task UpdateAsync(Performance performance)
        {
            try
            {
                await _performanceRepository.UpdateAsync(performance);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
