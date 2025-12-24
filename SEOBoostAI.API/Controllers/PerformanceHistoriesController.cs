using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.API.ViewModels.RequestModels;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services;
using SEOBoostAI.Service.Services.Interfaces;
using SEOBoostAI.Service.Utils;
using System.Security.Claims;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SEOBoostAI.API.Controllers
{
    [Route("api/performance-histories")]
    [ApiController]
    [Authorize]
    public class PerformanceHistoriesController : ControllerBase
    {
        private readonly IPerformanceHistoryService _performanceHistoryService;

        public PerformanceHistoriesController(IPerformanceHistoryService performanceHistoryService)
        {
            _performanceHistoryService = performanceHistoryService;
        }

        // GET: api/<PerformanceHistoriesController>
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] PerformanceHistoryGetAllRequestModel performanceHistoryGetAllRequestModel)
        {
            var userIdString = User.FindFirstValue("user_ID");
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }
            var result = await _performanceHistoryService.GetPerformanceHistorysWithPagination(performanceHistoryGetAllRequestModel.CurrentPage, performanceHistoryGetAllRequestModel.PageSize, userId);
            return Ok(new ResultModel<PaginationResult<List<PerformanceHistory>>>
            {
                Success = true,
                Message = "Performance histories retrieved successfully.",
                Data = result
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var result = await _performanceHistoryService.GetPerformanceHistoryByIdAsync(id);
                if (result == null)
                {
                    return NotFound();
                }
                return Ok(new ResultModel<PerformanceHistory>
                {
                    Success = true,
                    Message = "Performance history retrieved successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST api/<PerformanceHistoriesController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PerformanceHistoryRequestModel performanceHistoryViewModel)
        {
            var userIdString = User.FindFirstValue("user_ID");
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }
            try
            {
                var performanceHistory = await _performanceHistoryService.AnalysisPerformanceHistoryAsync(userId, performanceHistoryViewModel.Url, performanceHistoryViewModel.Strategy, performanceHistoryViewModel.FeatureId);
                return Ok(new ResultModel<PerformanceHistory>
                {
                    Success = true,
                    Message = "Performance history created successfully.",
                    Data = performanceHistory
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
            
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] PerformanceHistoryUpdateModel performanceHistoryUpdateModel)
        {
            var userIdString = User.FindFirstValue("user_ID");
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized();
            }

            try
            {
                var existingPerformanceHistory = await _performanceHistoryService.ReAnalyzePerformanceHistoryAsync(performanceHistoryUpdateModel.PerformanceHistoryId, userId, performanceHistoryUpdateModel.FeatureId);

                return Ok(new ResultModel<PerformanceHistory>
                {
                    Success = true,
                    Message = "Performance history updated successfully.",
                    Data = existingPerformanceHistory
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
