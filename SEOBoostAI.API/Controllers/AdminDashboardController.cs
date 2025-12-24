using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/admin/dashboard")]
	[ApiController]
	[Authorize(Roles = "Admin")]
	public class AdminDashboardController : ControllerBase
	{
		private readonly IAdminDashboardService _dashboardService;

		public AdminDashboardController(IAdminDashboardService dashboardService)
		{
			_dashboardService = dashboardService;
		}

		//Lấy số liệu tổng quan (Các ô to trên cùng)
		[HttpGet("overview")]
		public async Task<IActionResult> GetOverview()
		{
			try
			{
				var result = await _dashboardService.GetOverviewAsync();
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		//Lấy dữ liệu vẽ biểu đồ (type = week | month)
		[HttpGet("revenue-chart")]
		public async Task<IActionResult> GetRevenueChart([FromQuery] string type = "")
		{
			try
			{
				var result = await _dashboardService.GetRevenueChartAsync(type);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}
	}
}
