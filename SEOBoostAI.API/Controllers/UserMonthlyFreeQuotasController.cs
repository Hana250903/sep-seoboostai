using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/user-monthly-free-quotas")]
	[ApiController]
	[Authorize]
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

		[HttpPost("create-range")]
		public async Task<IActionResult> PostRange([FromBody] int userId)
		{
			var result = await _userMonthlyFreeQuotaService.CreateQuotaAsync(userId);
			return Ok(result);
        }

		[HttpGet("quota")]
		[Authorize]
		public async Task<IActionResult> GetMyQuota()
		{
			try
			{
				var userIdString = User.FindFirst("user_ID")?.Value;
				if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
				var userId = int.Parse(userIdString);

				var result = await _userMonthlyFreeQuotaService.GetUserQuotaInfoAsync(userId);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

        [HttpPut("update-monthly-limit")]
        public async Task<IActionResult> UpdateMonthlyLimit([FromBody] int newLimit)
        {
            if (newLimit < 0)
            {
                return BadRequest("Limit must be greater than or equal to 0.");
            }

            try
            {
                await _userMonthlyFreeQuotaService.UpdateLimitMonthlyAsync(newLimit);
                return Ok(new { Message = "Đã cập nhật thành công giới hạn hàng tháng cho tất cả người dùng và cấu hình hệ thống." });
            }
            catch (Exception ex)
            {
                // Log error here
                return StatusCode(500, new { Message = "Internal Server Error", Detail = ex.Message });
            }
        }
    }
}
