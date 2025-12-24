using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Controllers
{
    /// <summary>
    /// Controller cho AutoFix - Puppeteer audit và GitHub integration
    /// </summary>
    [Route("api/autofix")]
    [ApiController]
    [Authorize]
    public class AutoFixController : ControllerBase
    {
        private readonly IGitHubIntegrationService _gitService;
        private readonly IAutoFixService _autoFixService;

        public AutoFixController(
            IPuppeteerAuditService puppeteerService,
            IGitHubIntegrationService gitService,
            IAutoFixService autoFixService)
        {
            _gitService = gitService;
            _autoFixService = autoFixService;
        }

        /// <summary>
        /// Debug Repo: Kiểm tra cấu trúc repository GitHub
        /// </summary>
        [HttpGet("debug-repo")]
        public async Task<IActionResult> DebugRepo([FromQuery] string owner, [FromQuery] string repo, [FromQuery] string branch = null)
        {
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                return BadRequest(new { error = "Owner và Repo không được để trống" });

            try
            {
                var info = await _gitService.InspectRepoAsync(owner, repo, branch);
                return Ok(new ResultModel<RepoDebugInfo>
                {
                    Success = true,
                    Message = "Debug repo thành công",
                    Data = info
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResultModel<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Batch Fix: Fix tất cả issues từ một AnalysisCache cùng lúc
        /// </summary>
        [HttpPost("batch-fix")]
        public async Task<IActionResult> BatchFix([FromBody] BatchFixRequest req)
        {
            if (req == null || req.AnalysisCacheId <= 0)
                return BadRequest(new { error = "SessionId không hợp lệ" });

            if (string.IsNullOrEmpty(req.RepoOwner) || string.IsNullOrEmpty(req.RepoName))
                return BadRequest(new { error = "RepoOwner và RepoName không được để trống" });

            try
            {
                var result = await _autoFixService.BatchFixAsync(req);
                return Ok(new ResultModel<BatchFixResponse>
                {
                    Success = true,
                    Message = $"Đã fix {result.FixedCount}/{result.TotalIssues} issues",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResultModel<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Preview Issues: Xem trước các issues nằm ở file nào (không fix)
        /// </summary>
        [HttpPost("preview-issues")]
        public async Task<IActionResult> PreviewIssues([FromBody] PreviewIssuesRequest req)
        {
            if (req == null || req.AnalysisCacheId <= 0)
                return BadRequest(new { error = "SessionId không hợp lệ" });

            if (string.IsNullOrEmpty(req.RepoOwner) || string.IsNullOrEmpty(req.RepoName))
                return BadRequest(new { error = "RepoOwner và RepoName không được để trống" });

            try
            {
                var result = await _autoFixService.PreviewIssuesAsync(req);
                return Ok(new ResultModel<PreviewIssuesResponse>
                {
                    Success = true,
                    Message = $"Tìm thấy {result.TotalIssues} issues trong {result.Mappings.Count} files",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResultModel<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }
    }
}
