using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/features")]
	[ApiController]
	public class FeaturesController : ControllerBase
	{
		private readonly IFeatureService _featureService;

		public FeaturesController(IFeatureService featureService)
		{
			_featureService = featureService;
		}

		// GET: api/<FeaturesController>
		[HttpGet]
		public async Task<IEnumerable<Feature>> Get()
		{
			return await _featureService.GetFeaturesAsync();
		}

		[HttpGet("{currentPage}/{pageSize}")]
		public async Task<PaginationResult<List<Feature>>> Get(int currentPage, int pageSize)
		{
			return await _featureService.GetFeaturesWithPaginateAsync(currentPage, pageSize);
		}

		// GET api/<FeaturesController>/5
		[HttpGet("{id}")]
		public async Task<Feature> Get(int id)
		{
			return await _featureService.GetFeatureByIdAsync(id);
		}

		// POST api/<FeaturesController>
		[HttpPost]
		public async Task<int> Post([FromBody] Feature feature)
		{
			throw new NotImplementedException();
		}

		// PUT api/<FeaturesController>/
		[HttpPut]
		public async Task<int> Put([FromBody] Feature feature)
		{
			throw new NotImplementedException();
		}

		// DELETE api/<FeaturesController>/5
		[HttpDelete("{id}")]
		public async Task<bool> Delete(int id)
		{
			throw new NotImplementedException();
		}
	}
}
