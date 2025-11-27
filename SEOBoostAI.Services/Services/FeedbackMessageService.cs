using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
    public class FeedbackMessageService : IFeedbackMessageService
    {
        private readonly IFeedbackMessageRepository _feedbackMessageRepository;

        public FeedbackMessageService(IFeedbackMessageRepository feedbackMessageRepository)
        {
            _feedbackMessageRepository = feedbackMessageRepository;
        }

        public async Task<List<FeedbackMessage>> GetChatHistoryAsync(int feedbackId)
        {
            return await _feedbackMessageRepository.GetChatHistoryAsync(feedbackId);
        }
    }
}
