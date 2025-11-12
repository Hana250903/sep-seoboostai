using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/purchased-features")]
	[ApiController]
	public class PurchasedFeaturesController : ControllerBase
	{
		private readonly IPurchasedFeatureService _purchasedFeatureService;

		public PurchasedFeaturesController(IPurchasedFeatureService purchasedFeatureService)
		{
			_purchasedFeatureService = purchasedFeatureService;
		}

		// GET: api/<PurchasedFeaturesController>
		[HttpGet]
		public async Task<IEnumerable<PurchasedFeature>> Get()
		{
			return await _purchasedFeatureService.GetPurchasedFeaturesAsync();
		}

		[HttpGet("{currentPage}/{pageSize}")]
		public async Task<PaginationResult<List<PurchasedFeature>>> Get(int currentPage, int pageSize)
		{
			return await _purchasedFeatureService.GetPurchasedFeaturesWithPaginateAsync(currentPage, pageSize);
		}

		// GET api/<PurchasedFeaturesController>/5
		[HttpGet("{id}")]
		public async Task<PurchasedFeature> Get(int id)
		{
			return await _purchasedFeatureService.GetPurchasedFeatureByIdAsync(id);
		}

		// POST api/<PurchasedFeaturesController>
		[HttpPost]
		public async Task<IActionResult> Post([FromBody] PurchasedFeature purchasedFeature)
		{
			await _purchasedFeatureService.CreateAsync(purchasedFeature);
			return Ok(purchasedFeature);
        }

		// PUT api/<PurchasedFeaturesController>/
		[HttpPut]
		public async Task<IActionResult> Put([FromBody] PurchasedFeature purchasedFeature)
		{
			await _purchasedFeatureService.UpdateAsync(purchasedFeature);
			return Ok(purchasedFeature);
        }

		// DELETE api/<PurchasedFeaturesController>/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			await _purchasedFeatureService.DeleteAsync(id);
			return Ok();
        }
	}
}
