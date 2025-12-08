using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.API.ViewModels.RequestModels;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/features")]
	[ApiController]
	[Authorize]
	public class FeaturesController : ControllerBase
	{
		private readonly IFeatureService _featureService;

		public FeaturesController(IFeatureService featureService)
		{
			_featureService = featureService;
		}

		// GET: api/<FeaturesController>
		[HttpGet]
		[Authorize(Roles = "Member, Admin, Staff")]
		public async Task<IActionResult> GetAllFeatures()
		{
			try
			{
				// Gọi Service (Hàm này đã trả về List<FeatureDto>)
				var features = await _featureService.GetAllFeaturesAsync();

				return Ok(features);
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpGet("{currentPage}/{pageSize}")]
		public async Task<PaginationResult<List<Feature>>> Get(int currentPage, int pageSize)
		{
			return await _featureService.GetFeaturesWithPaginateAsync(currentPage, pageSize);
		}

		// GET api/<FeaturesController>/5
		[HttpGet("{id}")]
		[Authorize(Roles = "Admin, Staff")]
		public async Task<Feature> Get(int id)
		{
			return await _featureService.GetFeatureByIdAsync(id);
		}

		// POST api/<FeaturesController>
		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Post([FromBody] Feature feature)
		{
			await _featureService.CreateAsync(feature);
			return Ok(feature);
        }

		// PUT api/<FeaturesController>/
		[HttpPut]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Put([FromBody] Feature feature)
		{
			await _featureService.UpdateAsync(feature);
			return Ok(feature);
        }

		// DELETE api/<FeaturesController>/5
		[HttpDelete("{id}")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Delete(int id)
		{
			await _featureService.DeleteAsync(id);
			return Ok();
        }

		[HttpPut("{id}/benefits")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> UpdateBenefits(int id, [FromBody] UpdateFeatureBenefitsRequest request)
		{
			try
			{
				if (request.Benefits == null) return BadRequest("Danh sách lợi ích không được để trống.");

				await _featureService.UpdateFeatureBenefitsAsync(id, request.Benefits);

				return Ok(new { message = "Cập nhật quyền lợi gói thành công!" });
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}
	}
}
