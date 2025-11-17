using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.API.ViewModels.RequestModels;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services;
using SEOBoostAI.Service.Services.Interfaces;
using SEOBoostAI.Service.Ultils;
using System.Security.Claims;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SEOBoostAI.API.Controllers
{
    [Route("api/performance-histories")]
    [ApiController]
    public class PerformanceHistoriesController : ControllerBase
    {
        private readonly IPerformanceHistoryService _performanceHistoryService;
        private readonly ICurrentUserService _currentUserService;

        public PerformanceHistoriesController(IPerformanceHistoryService performanceHistoryService, ICurrentUserService currentUserService)
        {
            _performanceHistoryService = performanceHistoryService;
            _currentUserService = currentUserService;
        }

        // GET: api/<PerformanceHistoriesController>
        [HttpGet]
        public async Task<PaginationResult<List<PerformanceHistory>>> Get([FromQuery] PerformanceHistoryRequestModel performanceHistoryRequestModel)
        {
            //var userId = _currentUserService.GetUserId();
            return await _performanceHistoryService.GetPerformanceHistorysWithPagination(performanceHistoryRequestModel.CurrentPage, performanceHistoryRequestModel.PageSize, performanceHistoryRequestModel.UserId);
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
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST api/<PerformanceHistoriesController>
        //[Authorize]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PerformanceHistoryViewModel performanceHistoryViewModel)
        {
            //var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //if (!int.TryParse(userIdString, out int userId))
            //{
            //    return Unauthorized();
            //}

            var performanceHistory = await _performanceHistoryService.AnalysisPerformanceHistoryAsync(performanceHistoryViewModel.UserId, performanceHistoryViewModel.Url, performanceHistoryViewModel.Strategy);

            return Ok(performanceHistory);
        }

        //[Authorize]
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] PerformanceHistoryUpdateModel performanceHistoryUpdateModel)
        {
            //var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //if (!int.TryParse(userIdString, out int userId))
            //{
            //    return Unauthorized();
            //}

            var existingPerformanceHistory = await _performanceHistoryService.ReAnalyzePerformanceHistoryAsync(performanceHistoryUpdateModel.PerformanceHistoryId, performanceHistoryUpdateModel.UserId);

            return Ok(existingPerformanceHistory);
        }
    }
}
