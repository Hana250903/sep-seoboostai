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
		public async Task<IActionResult> Post([FromBody] Wallet wallet)
		{
			await _walletService.CreateAsync(wallet);
			return Ok(wallet);
        }

		// PUT api/<WalletsController>/
		[HttpPut]
		public async Task<IActionResult> Put([FromBody] Wallet wallet)
		{
			await _walletService.UpdateAsync(wallet);
			return Ok(wallet);
        }

		// DELETE api/<WalletsController>/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			await _walletService.DeleteAsync(id);
            return Ok();
        }
	}
}
