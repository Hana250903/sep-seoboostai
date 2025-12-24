using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.API.ViewModels.RequestModels;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/feature-informations")]
	[ApiController]
	[Authorize]
	public class FeatureInformationsController : ControllerBase
	{
		private readonly IFeatureInformationService _featureInformationService;

		public FeatureInformationsController(IFeatureInformationService featureInformationService)
		{
			_featureInformationService = featureInformationService;
		}

		// GET: api/feature-informations/feature/1
		[HttpGet("feature/{featureId}")]
		public async Task<IActionResult> GetByFeatureId(int featureId)
		{
			var result = await _featureInformationService.GetListByFeatureIdAsync(featureId);
			return Ok(result);
		}

		// POST: api/feature-informations
		[Authorize(Roles = "Admin")]
		[HttpPost]
		public async Task<IActionResult> Post([FromBody] CreateFeatureInfoRequest request)
		{
			try
			{
				var result = await _featureInformationService.CreateAsync(request);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		// PUT: api/feature-informations/5
		[Authorize(Roles = "Admin")]
		[HttpPut("{id}")]
		public async Task<IActionResult> Put(int id, [FromBody] UpdateFeatureInfoRequest request)
		{
			try
			{
				await _featureInformationService.UpdateAsync(id, request);
				return Ok(new { message = "Cập nhật thành công" });
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		// DELETE: api/feature-informations/5
		[Authorize(Roles = "Admin")]
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			try
			{
				await _featureInformationService.DeleteAsync(id);
				return Ok(new { message = "Xóa thành công" });
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}
	}
}