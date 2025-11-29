using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.API.ViewModels.RequestModels;
using SEOBoostAI.Service.Services.Interfaces;
using SEOBoostAI.Service.Ultils;
using System.Security.Claims;

namespace SEOBoostAI.API.Controllers
{
    // [Authorize] // Bật lại khi cần
    [Route("api/query-histories")]
    [ApiController]
    [Authorize]
    public class QueryHistoryController : ControllerBase
    {
        private readonly ITrendSearchService _trendSearchService;
        private readonly ICurrentUserService _currentUserService;

        public QueryHistoryController(ITrendSearchService trendSearchService, ICurrentUserService currentUserService)
        {
            _trendSearchService = trendSearchService;
            _currentUserService = currentUserService;
        }

        // GET: api/query-histories?CurrentPage=1&PageSize=10
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] QueryHistoryRequestModel requestModel)
        {
            try
            {
                // Lấy UserID (ưu tiên từ request, nếu không có thì lấy từ token/test)
                var userIdString = User.FindFirstValue("user_ID");
                if (!int.TryParse(userIdString, out int userId))
                {
                    return Unauthorized(new { message = "Token không hợp lệ hoặc không tìm thấy User ID." });
                }

                var result = await _trendSearchService.GetQueryHistoriesAsync(
                    userId,
                    requestModel.CurrentPage,
                    requestModel.PageSize
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}