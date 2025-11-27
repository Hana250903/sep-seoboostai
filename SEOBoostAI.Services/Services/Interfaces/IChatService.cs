using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IChatService
    {
        Task SendMessageAsync(int feedbackId, int senderId, string senderName, string content);
    }
}
