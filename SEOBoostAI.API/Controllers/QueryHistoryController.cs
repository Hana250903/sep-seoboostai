using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.API.ViewModels.RequestModels;
using SEOBoostAI.Service.Services.Interfaces;
using SEOBoostAI.Service.Ultils;

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
                int memberId = requestModel.UserId ?? _currentUserService.GetUserId();
                if (memberId == 0) memberId = 1; // Hardcode test nếu cần

                var result = await _trendSearchService.GetQueryHistoriesAsync(
                    memberId,
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