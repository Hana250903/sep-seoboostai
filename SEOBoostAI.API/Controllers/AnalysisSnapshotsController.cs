using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services;
using SEOBoostAI.Service.Services.Interfaces;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SEOBoostAI.API.Controllers
{
    [Route("api/analysis-snapshot")]
    [ApiController]
    public class AnalysisSnapshotsController : ControllerBase
    {
        private readonly IAnalysisSnapshotService _analysisSnapshotService;

        public AnalysisSnapshotsController(IAnalysisSnapshotService analysisSnapshotService)
        {
            _analysisSnapshotService = analysisSnapshotService;
        }

        // GET: api/<AnalysisSnapshotsController>
        [HttpGet("{currentPage}/{pageSize}")]
        public async Task<IActionResult> Get(int currentPage, int pageSize)
        {
            var result = await _analysisSnapshotService.GetAnalysisSnapshotsWithPagination(currentPage, pageSize);

            return Ok(new ResultModel<PaginationResult<List<AnalysisSnapshot>>>
            {
                Success = true,
                Message = "Analysis snapshots retrieved successfully.",
                Data = result
            });
        }

        // GET api/<AnalysisSnapshotsController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _analysisSnapshotService.GetAnalysisSnapshotByIdAsync(id);
            return Ok(new ResultModel<AnalysisSnapshot>
            {
                Success = true,
                Message = "Analysis snapshot retrieved successfully.",
                Data = result
            });
        }
    }
}
