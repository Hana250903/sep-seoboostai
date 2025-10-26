using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service;
using SEOBoostAI.Service.Services.Interfaces;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SEOBoostAI.API.Controllers
{
    [Route("api/element")]
    [ApiController]
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

        // POST api/<ElementsController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Element element)
        {
            throw new NotImplementedException();
        }

        // PUT api/<ElementsController>/
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] Element element)
        {
            throw new NotImplementedException();
        }

        // DELETE api/<ElementsController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            throw new NotImplementedException();
        }
    }
}
