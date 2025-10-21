using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/feedback")]
	[ApiController]
	public class FeedbacksController : ControllerBase
	{
		private readonly IFeedbackService _feedbackService;

		public FeedbacksController(IFeedbackService feedbackService)
		{
			_feedbackService = feedbackService;
		}

		// GET: api/<FeedbacksController>
		[HttpGet]
		public async Task<IEnumerable<Feedback>> Get()
		{
			return await _feedbackService.GetFeedbacksAsync();
		}

		[HttpGet("{currentPage}/{pageSize}")]
		public async Task<PaginationResult<List<Feedback>>> Get(int currentPage, int pageSize)
		{
			return await _feedbackService.GetFeedbacksWithPaginateAsync(currentPage, pageSize);
		}

		// GET api/<FeedbacksController>/5
		[HttpGet("{id}")]
		public async Task<Feedback> Get(int id)
		{
			return await _feedbackService.GetFeedbackByIdAsync(id);
		}

		// POST api/<FeedbacksController>
		[HttpPost]
		public async Task<int> Post([FromBody] Feedback feedback)
		{
			return await _feedbackService.CreateAsync(feedback);
		}

		// PUT api/<FeedbacksController>/
		[HttpPut]
		public async Task<int> Put([FromBody] Feedback feedback)
		{
			return await _feedbackService.UpdateAsync(feedback);
		}

		// DELETE api/<FeedbacksController>/5
		[HttpDelete("{id}")]
		public async Task<bool> Delete(int id)
		{
			return await _feedbackService.DeleteAsync(id);
		}
	}
}
