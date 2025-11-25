using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IFeedbackMessageService
    {
        Task<List<FeedbackMessage>> GetHistoryAsync(int feedbackId);
    }
}
