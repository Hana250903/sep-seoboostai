using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.API.ViewModels.RequestModels;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services;
using SEOBoostAI.Service.Services.Interfaces;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SEOBoostAI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PerformanceHistoriesController : ControllerBase
    {
        private readonly IPerformanceHistoryService _performanceHistoryService;
        private readonly IUserService _userService;

        public PerformanceHistoriesController(IPerformanceHistoryService performanceHistoryService, IUserService userService)
        {
            _performanceHistoryService = performanceHistoryService;
            _userService = userService;
        }

        // GET: api/<PerformanceHistoriesController>
        [HttpGet("{currentPage}/{pageSize}")]
        public async Task<PaginationResult<List<PerformanceHistory>>> Get(int currentPage, int pageSize)
        {
            return await _performanceHistoryService.GetPerformanceHistorysWithPagination(currentPage, pageSize);
        }

        // POST api/<PerformanceHistoriesController>
        [HttpPost]
        public async Task<PerformanceHistory> Post([FromBody] PerformanceHistoryViewModel performanceHistoryViewModel)
        {
            return await _performanceHistoryService.AnalysisPerformanceHistoryAsync(performanceHistoryViewModel.UserId, performanceHistoryViewModel.Url, performanceHistoryViewModel.Url);
        }

        // PUT api/<PerformanceHistoriesController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<PerformanceHistoriesController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
