using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IChatNotifier
    {
        Task SendNewMessage(string roomId, string user, string message, DateTime time);
        Task NotifyAdminNewTicket(int feedbackId);
    }
}
