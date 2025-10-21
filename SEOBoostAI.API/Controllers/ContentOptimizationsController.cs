using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;
using System.Threading.Tasks;


namespace SEOBoostAI.API.Controllers
{
	[Route("api/content-optimizations")]
	[ApiController]
	public class ContentOptimizationsController : ControllerBase
	{
		private readonly IContentOptimizationService _contentOptimizationService;
		public ContentOptimizationsController(IContentOptimizationService contentOptimizationService)
		{
			_contentOptimizationService = contentOptimizationService;
		}

		// GET: api/<ContentOptimizationsController>
		[HttpGet]
		public async Task<IEnumerable<ContentOptimization>> Get()
		{
			return await _contentOptimizationService.GetContentOptimizationsAsync();
		}

		[HttpGet("{currentPage}/{pageSize}")]
		public async Task<PaginationResult<List<ContentOptimization>>> Get(int currentPage, int pageSize)
		{
			return await _contentOptimizationService.GetContentOptimizationsWithPaginateAsync(currentPage, pageSize);
		}

		// GET api/<ContentOptimizationsController>/5
		[HttpGet("{id}")]
		public async Task<ContentOptimization> Get(int id)
		{
			return await _contentOptimizationService.GetContentOptimizationByIdAsync(id);
		}

		// POST api/<ContentOptimizationsController>
		[HttpPost]
		public async Task<int> Post([FromBody] ContentOptimization contentOptimization)
		{
			return await _contentOptimizationService.CreateAsync(contentOptimization);
		}

		// PUT api/<ContentOptimizationsController>/
		[HttpPut]
		public async Task<int> Put([FromBody] ContentOptimization contentOptimization)
		{
			return await _contentOptimizationService.UpdateAsync(contentOptimization);
		}

		// DELETE api/<ContentOptimizationsController>/5
		[HttpDelete("{id}")]
		public async Task<bool> Delete(int id)
		{
			return await _contentOptimizationService.DeleteAsync(id);
		}
	}
}
