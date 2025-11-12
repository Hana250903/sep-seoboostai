using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/user-monthly-free-quotas")]
	[ApiController]
	public class UserMonthlyFreeQuotasController : ControllerBase
	{
		private readonly IUserMonthlyFreeQuotaService _userMonthlyFreeQuotaService;

		public UserMonthlyFreeQuotasController(IUserMonthlyFreeQuotaService userMonthlyFreeQuotaService)
		{
			_userMonthlyFreeQuotaService = userMonthlyFreeQuotaService;
		}

		// GET: api/<UserMonthlyFreeQuotasController>
		[HttpGet]
		public async Task<IEnumerable<UserMonthlyFreeQuota>> Get()
		{
			return await _userMonthlyFreeQuotaService.GetUserMonthlyFreeQuotasAsync();
		}

		[HttpGet("{currentPage}/{pageSize}")]
		public async Task<PaginationResult<List<UserMonthlyFreeQuota>>> Get(int currentPage, int pageSize)
		{
			return await _userMonthlyFreeQuotaService.GetUserMonthlyFreeQuotasWithPaginateAsync(currentPage, pageSize);
		}

		// GET api/<UserMonthlyFreeQuotasController>/5
		[HttpGet("{id}")]
		public async Task<UserMonthlyFreeQuota> Get(int id)
		{
			return await _userMonthlyFreeQuotaService.GetUserMonthlyFreeQuotaByIdAsync(id);
		}

		// POST api/<UserMonthlyFreeQuotasController>
		[HttpPost]
		public async Task<IActionResult> Post([FromBody] UserMonthlyFreeQuota userMonthlyFreeQuota)
		{
			await _userMonthlyFreeQuotaService.CreateAsync(userMonthlyFreeQuota);
			return Ok(userMonthlyFreeQuota);
        }

		// PUT api/<UserMonthlyFreeQuotasController>/
		[HttpPut]
		public async Task<IActionResult> Put([FromBody] UserMonthlyFreeQuota userMonthlyFreeQuota)
		{
			await _userMonthlyFreeQuotaService.UpdateAsync(userMonthlyFreeQuota);
			return Ok(userMonthlyFreeQuota);
        }

		// DELETE api/<UserMonthlyFreeQuotasController>/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			await _userMonthlyFreeQuotaService.DeleteAsync(id);
			return Ok();
        }
	}
}
