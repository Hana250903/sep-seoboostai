using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/feedbacks")]
	[ApiController]
	public class FeedbacksController : ControllerBase
	{
		private readonly IFeedbackService _feedbackService;
        private readonly IFeedbackMessageService _feedbackMessageService;
        private readonly IChatNotifier _chatNotifier;

        public FeedbacksController(IFeedbackService feedbackService, IFeedbackMessageService feedbackMessageService, IChatNotifier chatNotifier)
		{
			_feedbackService = feedbackService;
            _feedbackMessageService = feedbackMessageService;
            _chatNotifier = chatNotifier;
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

		[HttpGet("message-histories/{userId}")]
		public async Task<IActionResult> GetMessageHistory(int userId)
		{
			var history = await _feedbackService.GetFeedbacksByUserIdAsync(userId);
			return Ok(history);
        }

		[HttpGet("chat-histories/{feedbackId}")]
		public async Task<IActionResult> GetChatHistory(int feedbackId)
		{
			var history = await _feedbackMessageService.GetChatHistoryAsync(feedbackId);
			return Ok(history);
        }

        // POST api/<FeedbacksController>
        [HttpPost]
		public async Task<IActionResult> Post([FromBody] Feedback feedback)
		{
            await _feedbackService.CreateAsync(feedback);
            await _chatNotifier.NotifyAdminNewTicket(feedback.FeedbackID);
            return Ok(new { id = feedback.FeedbackID });
        }

		// PUT api/<FeedbacksController>/
		[HttpPut]
		public async Task<IActionResult> Put([FromBody] Feedback feedback)
		{
            await _feedbackService.UpdateAsync(feedback);
			return Ok(feedback);
        }

		// DELETE api/<FeedbacksController>/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
            await _feedbackService.DeleteAsync(id);
			return Ok();
        }
	}
}
