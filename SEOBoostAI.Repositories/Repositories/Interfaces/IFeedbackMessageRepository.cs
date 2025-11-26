using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories.Interfaces
{
    public interface IFeedbackMessageRepository : IGenericRepository<FeedbackMessage>
    {
        Task<List<FeedbackMessage>> GetChatHistoryAsync(int feedbackId);
    }
}
