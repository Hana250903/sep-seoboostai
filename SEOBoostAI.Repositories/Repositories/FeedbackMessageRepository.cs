using Microsoft.EntityFrameworkCore;
using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories
{
    public class FeedbackMessageRepository : GenericRepository<FeedbackMessage>, IFeedbackMessageRepository
    {
        public FeedbackMessageRepository(SEP_SEOBoostAIContext context) : base(context)
        {
        }

        public async Task<List<FeedbackMessage>> GetChatHistoryAsync(int feedbackId)
        {
            return await _context.Set<FeedbackMessage>().Where(fm => fm.FeedbackID == feedbackId).OrderBy(fm => fm.CreatedAt).ToListAsync();
        }
    }
}
