using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.PerformanceAnalysis
{
    public class AnalysisSnapshotService : IAnalysisSnapshotService
    {
        private readonly IAnalysisSnapshotRepository _analysisSnapshotRepository;

        public AnalysisSnapshotService(IAnalysisSnapshotRepository analysisSnapshotRepository)
        {
            _analysisSnapshotRepository = analysisSnapshotRepository;
        }

        public async Task<PaginationResult<List<AnalysisSnapshot>>> GetAnalysisSnapshotsWithPagination(int currentPage, int pageSize)
        {
            return await _analysisSnapshotRepository.GetAnalysisSnapshotsWithPagination(currentPage, pageSize);
        }

        public async Task<AnalysisSnapshot> GetAnalysisSnapshotByIdAsync(int id)
        {
            return await _analysisSnapshotRepository.GetByIdAsync(id);
        }
    }
}
