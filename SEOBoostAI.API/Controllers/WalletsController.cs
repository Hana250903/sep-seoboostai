using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/wallets")]
	[ApiController]
	[Authorize]
	public class WalletsController : ControllerBase
	{
		private readonly IWalletService _walletService;

		public WalletsController(IWalletService walletService)
		{
			_walletService = walletService;
		}
		// GET: api/<WalletsController>

		[HttpGet("{currentPage}/{pageSize}")]
		public async Task<PaginationResult<List<Wallet>>> Get(int currentPage, int pageSize)
		{
			return await _walletService.GetWalletsWithPaginateAsync(currentPage, pageSize);
		}

		// GET api/<WalletsController>/5
		[HttpGet]
		public async Task<Wallet> Get()
		{
			var userIdString = User.FindFirst("user_ID")?.Value;
			if (string.IsNullOrEmpty(userIdString))
			{
				return null;
			}

			var userId = int.Parse(userIdString);

			return await _walletService.GetWalletByUserIdAsync(userId);
		}

		[HttpGet("{userId}")]
		public async Task<Wallet> Get(int userId)
		{
			return await _walletService.GetWalletByUserIdAsync(userId);
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
