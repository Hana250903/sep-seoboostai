using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;
using System.Security.Claims;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/feedbacks")]
	[ApiController]
	[Authorize]
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
		public async Task<IActionResult> Get(int currentPage, int pageSize)
		{
			try
			{
                var result = await _feedbackService.GetFeedbacksWithPaginateAsync(currentPage, pageSize);
				return Ok(new ResultModel<PaginationResult<List<Feedback>>>
				{
					Success = true,
					Message = "Feedbacks retrieved successfully.",
					Data = result
				});
            }
			catch (Exception ex)
			{
				return StatusCode(500, "Internal server error: " + ex.Message);
            }
		}

		// GET api/<FeedbacksController>/5
		[HttpGet("{id}")]
		public async Task<IActionResult> Get(int id)
		{
			try
			{
				var result = await _feedbackService.GetFeedbackByIdAsync(id);
				return Ok(new ResultModel<Feedback>
				{
					Success = true,
					Message = "Feedback retrieved successfully.",
					Data = result
				});
            }
			catch(Exception ex)
			{
				return StatusCode(500, new ResultModel<Feedback>
				{
					Success = false,
					Message = "Error retrieving feedback: " + ex.Message,
					Data = null
                });
			}
		}

		[HttpGet("message-histories")]
		public async Task<IActionResult> GetMessageHistory()
		{
			var userIdString = User.FindFirstValue("user_ID");
			if (!int.TryParse(userIdString, out int userId))
			{
				return Unauthorized(new ResultModel<List<Feedback>>
				{
					Success = false,
					Message = "Invalid user ID.",
					Data = null
				});
            }
            try
			{
                var history = await _feedbackService.GetFeedbacksByUserIdAsync(userId);
                return Ok(new ResultModel<List<Feedback>>
                {
                    Success = true,
                    Message = "Feedback messages retrieved successfully.",
                    Data = history
                });
            }
			catch (Exception ex)
			{
				return StatusCode(500, new ResultModel<List<Feedback>>
				{
					Success = false,
					Message = "Error retrieving feedback messages: " + ex.Message,
					Data = null
				});
			}
        }

		[HttpGet("chat-histories/{feedbackId}")]
		public async Task<IActionResult> GetChatHistory(int feedbackId)
		{
			try
			{
                var history = await _feedbackMessageService.GetChatHistoryAsync(feedbackId);
                return Ok(new ResultModel<List<FeedbackMessage>>
                {
                    Success = true,
                    Message = "Chat history retrieved successfully.",
                    Data = history
                });
            }
			catch (Exception ex)
			{
				return StatusCode(500, new ResultModel<List<FeedbackMessage>>
				{
					Success = false,
					Message = "Error retrieving chat history: " + ex.Message,
					Data = null
				});
            }
        }

        // POST api/<FeedbacksController>
        [HttpPost]
		public async Task<IActionResult> Post([FromBody] Feedback feedback)
		{
			try
			{
                await _feedbackService.CreateAsync(feedback);
                await _chatNotifier.NotifyAdminNewTicket(feedback.FeedbackID);
                return Ok(new {
					Success = true,
					Message = "Feedback created successfully.",
                    id = feedback.FeedbackID });
            }
			catch (Exception ex)
			{
				return StatusCode(500, new ResultModel<Feedback>
				{
					Success = false,
					Message = "Error creating feedback: " + ex.Message,
					Data = null
				});
            }
        }

		// PUT api/<FeedbacksController>/
		[HttpPut]
		public async Task<IActionResult> Put([FromBody] Feedback feedback)
		{
			try
			{
                await _feedbackService.UpdateAsync(feedback);
                return Ok(new ResultModel<Feedback>
                {
                    Success = true,
                    Message = "Feedback updated successfully.",
                    Data = feedback
                });
            }
			catch (Exception ex)
			{
				return StatusCode(500, new ResultModel<Feedback>
				{
					Success = false,
					Message = "Error updating feedback: " + ex.Message,
					Data = null
				});
            }
        }

		// DELETE api/<FeedbacksController>/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			try
			{
                await _feedbackService.DeleteAsync(id);
                return Ok(new
				{
					Success = true,
					Message = "Feedback deleted successfully."
                });
            }
			catch (Exception ex)
			{
				return StatusCode(500, new ResultModel<Feedback>
				{
					Success = false,
					Message = "Error deleting feedback: " + ex.Message,
					Data = null
				});
            }
        }
	}
}
