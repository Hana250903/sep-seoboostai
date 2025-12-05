using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;
using SEOBoostAI.Service.Services.PerformanceAnalysis;

namespace SEOBoostAI.API.Controllers
{
    [Route("api/metadata-analysis")]
    [ApiController]
    public class MetaDataAnalysisController : ControllerBase
    {
        private readonly IMetaDataAnalysisService _metaDataAnalysisService;

        public MetaDataAnalysisController(IMetaDataAnalysisService metaDataAnalysisService)
        {
            _metaDataAnalysisService = metaDataAnalysisService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var analyses = await _metaDataAnalysisService.GetAllMetaDataAnalysesAsync();
            return Ok(new ResultModel<List<MetaDataAnalysis>>
            {
                Success = true,
                Message = "Meta data analyses retrieved successfully.",
                Data = analyses
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var analysis = await _metaDataAnalysisService.GetMetaDataAnalysisWithIdAsync(id);
            if (analysis == null)
            {
                return NotFound(new ResultModel<MetaDataAnalysis>
                {
                    Success = false,
                    Message = "Meta data analysis not found.",
                    Data = null
                });
            }
            return Ok(new ResultModel<MetaDataAnalysis>
            {
                Success = true,
                Message = "Meta data analysis retrieved successfully.",
                Data = analysis
            });
        }

        [HttpGet("analyze-cache/{analysisCacheId}")]
        public async Task<IActionResult> GetByAnalysisCacheId(int analysisCacheId)
        {
            var analysis = await _metaDataAnalysisService.GetMetaDataAnalysisByAnalysisCacheIdAsync(analysisCacheId);
            if (analysis == null)
            {
                return NotFound(new ResultModel<MetaDataAnalysis>
                {
                    Success = false,
                    Message = "Meta data analysis not found.",
                    Data = null
                });
            }
            return Ok(new ResultModel<MetaDataAnalysis>
            {
                Success = true,
                Message = "Meta data analysis retrieved successfully.",
                Data = analysis
            });
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromBody] int metadataAnalysisId)
        {
            try
            {
                var result = await _metaDataAnalysisService.AnalyzeMetaDataAsync(metadataAnalysisId);
                return Ok(new ResultModel<MetaDataAnalysis>
                {
                    Success = true,
                    Message = "Meta data analysis completed successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResultModel<MetaDataAnalysis>
                {
                    Success = false,
                    Message = $"An error occurred during meta data analysis: {ex.Message}",
                    Data = null
                });
            }
        }
    }
}
