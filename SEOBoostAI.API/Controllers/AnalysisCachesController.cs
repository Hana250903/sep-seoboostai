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
    [Route("api/analysisCache")]
    [ApiController]
    public class AnalysisCachesController : ControllerBase
    {
        private readonly IAnalysisCacheService _analysisCacheService;

        public AnalysisCachesController(IAnalysisCacheService analysisCacheService)
        {
            _analysisCacheService = analysisCacheService;
        }

        // GET: api/<AnalysisCachesController>
        [HttpGet]
        public async Task<IEnumerable<AnalysisCache>> Get()
        {
            return await _analysisCacheService.GetAnalysisCachesAsync();
        }

        [HttpGet("{currentPage}/{pageSize}")]
        public async Task<PaginationResult<List<AnalysisCache>>> Get(int currentPage, int pageSize)
        {
            return await _analysisCacheService.GetAnalysisCachesWithPaginateAsync(currentPage, pageSize);
        }

        // GET api/<AnalysisCachesController>/5
        [HttpGet("{id}")]
        public async Task<AnalysisCache> Get(int id)
        {
            return await _analysisCacheService.GetAnalysisCacheByIdAsync(id);
        }

        // POST api/<AnalysisCachesController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AnalysisCache analysisCache)
        {
            await _analysisCacheService.CreateAsync(analysisCache);
            return Ok(analysisCache);
        }

        // PUT api/<AnalysisCachesController>
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] AnalysisCache analysisCache)
        {
            await _analysisCacheService.UpdateAsync(analysisCache);
            return Ok(analysisCache);
        }

        // DELETE api/<AnalysisCachesController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _analysisCacheService.DeleteAsync(id);
            return Ok();
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalysisAnalysisCache ([FromBody] AnalyzeUrlViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrEmpty(model.Strategy))
            {
                model.Strategy = "desktop"; // Giá trị mặc định
            }

            try
            {
                var result = await _analysisCacheService.AnalyzeAndSaveAnalysisCacheAsync(
                    model.Url,
                    model.Strategy
                );

                // Trả về đối tượng AnalysisCache vừa được tạo
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
