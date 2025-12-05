using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service;
using SEOBoostAI.Service.Services.Interfaces;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SEOBoostAI.API.Controllers
{
    [Route("api/elements")]
    [ApiController]
    [Authorize]
    public class ElementsController : ControllerBase
    {
        private readonly IElementService _elementService;

        public ElementsController(IElementService elementService)
        {
            _elementService = elementService;
        }

        // GET: api/<ElementsController>
        [HttpGet]
        public async Task<IEnumerable<Element>> Get()
        {
            return await _elementService.GetElementsAsync();
        }

        [HttpGet("{currentPage}/{pageSize}")]
        public async Task<PaginationResult<List<Element>>> Get(int currentPage, int pageSize)
        {
            return await _elementService.GetElementsWithPaginateAsync(currentPage,pageSize);
        }

        // GET api/<ElementsController>/5
        [HttpGet("{id}")]
        public async Task<Element> Get(int id)
        {
            return await _elementService.GetElementByIdAsync(id);
        }

        [HttpGet("analysis/{analysisCacheId}")]
        public async Task<IActionResult> GetElementsByAnalysisCacheId(int analysisCacheId)
        {
            try
            {
                var elements = await _elementService.GetElementsByAnalysisCacheIdAsync(analysisCacheId);
                return Ok(new ResultModel<List<Element>>
                {
                    Success = true,
                    Message = "Elements retrieved successfully.",
                    Data = elements
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResultModel<List<Element>>
                {
                    Success = false,
                    Message = $"An error occurred while retrieving elements: {ex.Message}",
                    Data = null
                });
            }
        }

        // POST api/<ElementsController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Element element)
        {
            await _elementService.CreateAsync(element);
            return Ok(element);
        }

        // PUT api/<ElementsController>/
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] Element element)
        {
            await _elementService.UpdateAsync(element);
            return Ok(element);
        }

        // DELETE api/<ElementsController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _elementService.DeleteAsync(id);
            return Ok();
        }

        [HttpGet("suggestion/{analysisCacheID}")]
        public async Task<IActionResult> Suggestion(int analysisCacheID)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _elementService.Suggestion(analysisCacheID);
                return Ok(new ResultModel<List<ElementViewModel>>
                {
                    Success = true,
                    Message = "Suggestions generated successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResultModel<List<ElementViewModel>>
                {
                    Success = false,
                    Message = $"An error occurred while generating suggestions: {ex.Message}",
                    Data = null
                });
            }
        }
    }
}
