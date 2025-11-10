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
		public async Task<IEnumerable<ContentOptimizationDto>> Get()
		{
			return await _contentOptimizationService.GetContentOptimizationsAsync();
		}

		[HttpGet("{currentPage}/{pageSize}")]
		public async Task<PaginationResult<List<ContentOptimizationDto>>> Get(int currentPage, int pageSize)
		{
			return await _contentOptimizationService.GetContentOptimizationsWithPaginateAsync(currentPage, pageSize);
		}

		// GET api/<ContentOptimizationsController>/5
		[HttpGet("{id}")]
		public async Task<ContentOptimizationDto> Get(int id)
		{
			return await _contentOptimizationService.GetContentOptimizationByIdAsync(id);
		}

		// POST api/<ContentOptimizationsController>
		//[HttpPost]
		//public async Task<int> Post([FromBody] ContentOptimization contentOptimization)
		//{
		//	throw new NotImplementedException();
		//}

		// PUT api/<ContentOptimizationsController>/
		[HttpPut]
		public async Task<int> Put([FromBody] ContentOptimization contentOptimization)
		{
            throw new NotImplementedException();
        }

		// DELETE api/<ContentOptimizationsController>/5
		[HttpDelete("{id}")]
		public async Task<bool> Delete(int id)
		{
            throw new NotImplementedException();
        }

		[HttpPost]
		public async Task<IActionResult> Post([FromBody] OptimizeRequestDto requestDto)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			try
			{
				// Gọi phương thức service mới
				ContentOptimizationDto result = await _contentOptimizationService.OptimizeAndCreateAsync(requestDto);

				// Trả về kết quả
				return CreatedAtAction(nameof(Get), new { id = result.ContentOptimizationID }, result);
			}
			catch (Exception ex)
			{
				// Báo lỗi nếu có
				return StatusCode(500, $"Internal server error: {ex.Message}");
			}
		}
	}
}
