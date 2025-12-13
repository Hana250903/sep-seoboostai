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
    public class AutoFixController : ControllerBase
    {
        private readonly IPuppeteerAuditService _puppeteerService;
        private readonly IGitHubIntegrationService _gitService;
        private readonly IAutoFixService _autoFixService;

        public AutoFixController(
            IPuppeteerAuditService puppeteerService,
            IGitHubIntegrationService gitService,
            IAutoFixService autoFixService)
        {
            _puppeteerService = puppeteerService;
            _gitService = gitService;
            _autoFixService = autoFixService;
        }

        /// <summary>
        /// Scan URL bằng Puppeteer và lưu kết quả vào database
        /// </summary>
        /// <param name="req">ScanRequest với URL cần scan</param>
        /// <returns>AnalysisCacheID của session đã tạo</returns>
        [HttpPost("scan-custom")]
        [AllowAnonymous]
        public async Task<IActionResult> ScanCustom([FromBody] ScanRequest req)
        {
            if (string.IsNullOrEmpty(req?.Url))
                return BadRequest(new { error = "URL không được để trống" });

            try
            {
                var sessionId = await _puppeteerService.RunAuditAsync(req.Url);
                return Ok(new ResultModel<object>
                {
                    Success = true,
                    Message = "Quét thành công bằng Puppeteer!",
                    Data = new { sessionId }
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
        /// Debug Scan: Xem Puppeteer đang đọc được gì từ trang web (không lưu vào DB)
        /// </summary>
        [HttpGet("debug-scan")]
        [AllowAnonymous]
        public async Task<IActionResult> DebugScan([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url))
                return BadRequest(new { error = "URL không được để trống" });

            try
            {
                var result = await _puppeteerService.DebugScanAsync(url);
                return Ok(new ResultModel<object>
                {
                    Success = true,
                    Message = "Debug scan thành công",
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
        /// Debug Repo: Kiểm tra cấu trúc repository GitHub
        /// </summary>
        [HttpGet("debug-repo")]
        [AllowAnonymous]
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
        [AllowAnonymous]
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
        [AllowAnonymous]
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

        /// <summary>
        /// Detect Repo Structure: Phát hiện cấu trúc project (vite, nextjs, monorepo, etc.)
        /// </summary>
        [HttpGet("detect-structure")]
        [AllowAnonymous]
        public async Task<IActionResult> DetectStructure([FromQuery] string owner, [FromQuery] string repo)
        {
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                return BadRequest(new { error = "Owner và Repo không được để trống" });

            try
            {
                var structure = await _gitService.DetectRepoStructureAsync(owner, repo);
                return Ok(new ResultModel<RepoStructure>
                {
                    Success = true,
                    Message = $"Detected project type: {structure.ProjectType}",
                    Data = structure
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
