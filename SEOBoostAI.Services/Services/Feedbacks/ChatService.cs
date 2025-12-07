using Microsoft.AspNetCore.SignalR;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Feedbacks
{
    public class ChatService : IChatService
    {
        private readonly IFeedbackMessageRepository _feedbackMessageRepository;
        private readonly IChatNotifier _chatNotifier;
        private readonly IUnitOfWork _unitOfWork;

        public ChatService(IFeedbackMessageRepository feedbackMessageRepository, IChatNotifier chatNotifier, IUnitOfWork unitOfWork)
        {
            _feedbackMessageRepository = feedbackMessageRepository;
            _chatNotifier = chatNotifier;
            _unitOfWork = unitOfWork;
        }

        public async Task SendMessageAsync(int feedbackId, int senderId, string senderName, string content)
        {
            var msg = new FeedbackMessage
            {
                FeedbackID = feedbackId,
                SenderID = senderId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
            };

            await _feedbackMessageRepository.CreateAsync(msg);
            await _unitOfWork.SaveChangesAsync();

            await _chatNotifier.SendNewMessage(feedbackId.ToString(), senderName, content, msg.CreatedAt);
        }
    }
}
