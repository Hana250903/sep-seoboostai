using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/questiondatas")]
	[ApiController]
	public class QuestionDatasController : ControllerBase
	{
		private readonly IQuestionDataService _questionDataService;

		public QuestionDatasController(IQuestionDataService questionDataService)
		{
			_questionDataService = questionDataService;
		}

		// GET: api/<QuestionDatasController>
		[HttpGet]
		public async Task<IEnumerable<QuestionData>> Get()
		{
			return await _questionDataService.GetQuestionDatasAsync();
		}

		[HttpGet("{currentPage}/{pageSize}")]
		public async Task<PaginationResult<List<QuestionData>>> Get(int currentPage, int pageSize)
		{
			return await _questionDataService.GetQuestionDatasWithPaginateAsync(currentPage, pageSize);
		}

		// GET api/<QuestionDatasController>/5
		[HttpGet("{id}")]
		public async Task<QuestionData> Get(int id)
		{
			return await _questionDataService.GetQuestionDataByIdAsync(id);
		}

		// POST api/<QuestionDatasController>
		[HttpPost]
		public async Task<int> Post([FromBody] QuestionData questionData)
		{
			throw new NotImplementedException();
		}

		// PUT api/<QuestionDatasController>/
		[HttpPut]
		public async Task<int> Put([FromBody] QuestionData questionData)
		{
			throw new NotImplementedException();
		}

		// DELETE api/<QuestionDatasController>/5
		[HttpDelete]
		public async Task<bool> Delete(int id)
		{
			throw new NotImplementedException();
		}
	}
}
