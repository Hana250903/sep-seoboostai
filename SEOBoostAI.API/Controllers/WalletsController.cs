using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/wallets")]
	[ApiController]
	public class WalletsController : ControllerBase
	{
		private readonly IWalletService _walletService;

		public WalletsController(IWalletService walletService)
		{
			_walletService = walletService;
		}
		// GET: api/<WalletsController>
		[HttpGet]
		public async Task<IEnumerable<Wallet>> Get()
		{
			return await _walletService.GetWalletsAsync();
		}

		[HttpGet("{currentPage}/{pageSize}")]
		public async Task<PaginationResult<List<Wallet>>> Get(int currentPage, int pageSize)
		{
			return await _walletService.GetWalletsWithPaginateAsync(currentPage, pageSize);
		}

		// GET api/<WalletsController>/5
		[HttpGet("{id}")]
		public async Task<Wallet> Get(int id)
		{
			return await _walletService.GetWalletByIdAsync(id);
		}

		// POST api/<WalletsController>
		[HttpPost]
		public async Task<int> Post([FromBody] Wallet wallet)
		{
			throw new NotImplementedException();
		}

		// PUT api/<WalletsController>/
		[HttpPut]
		public async Task<int> Put([FromBody] Wallet wallet)
		{
			throw new NotImplementedException();
		}

		// DELETE api/<WalletsController>/5
		[HttpDelete("{id}")]
		public async Task<bool> Delete(int id)
		{
			throw new NotImplementedException();
		}
	}
}
