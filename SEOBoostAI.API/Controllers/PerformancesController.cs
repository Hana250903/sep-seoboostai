using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services;
using SEOBoostAI.Service.Services.Interfaces;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SEOBoostAI.API.Controllers
{
    [Route("api/performance")]
    [ApiController]
    public class PerformancesController : ControllerBase
    {
        private readonly IPerformanceService _performanceService;

        public PerformancesController(IPerformanceService performanceService)
        {
            _performanceService = performanceService;
        }

        // GET: api/<PerformancesController>
        [HttpGet]
        public async Task<IEnumerable<Performance>> Get()
        {
            return await _performanceService.GetPerformancesAsync();
        }

        [HttpGet("{currentPage}/{pageSize}")]
        public async Task<PaginationResult<List<Performance>>> Get(int currentPage, int pageSize)
        {
            return await _performanceService.GetPerformancesWithPaginateAsync(currentPage, pageSize);
        }

        // GET api/<PerformancesController>/5
        [HttpGet("{id}")]
        public async Task<Performance> Get(int id)
        {
            return await _performanceService.GetPerformanceByIdAsync(id);
        }

        // POST api/<PerformancesController>
        [HttpPost]
        public async Task<int> Post([FromBody] Performance performance)
        {
            return await _performanceService.CreateAsync(performance);
        }

        // PUT api/<PerformancesController>
        [HttpPut]
        public async Task<int> Put([FromBody] Performance performance)
        {
            return await _performanceService.UpdateAsync(performance);
        }

        // DELETE api/<PerformancesController>/5
        [HttpDelete("{id}")]
        public async Task<bool> Delete(int id)
        {
            return await _performanceService.DeleteAsync(id);
        }
    }
}
