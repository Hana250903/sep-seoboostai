using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IAnalysisSnapshotService
    {
        Task<PaginationResult<List<AnalysisSnapshot>>> GetAnalysisSnapshotsWithPagination(int currentPage, int pageSize);
        Task<AnalysisSnapshot> GetAnalysisSnapshotByIdAsync(int id);
    }
}
