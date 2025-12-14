using Microsoft.AspNetCore.SignalR;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Hubs
{
    public class SignalRChatNotifier : IChatNotifier
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public SignalRChatNotifier(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendNewMessage(string roomId, string user, string message, DateTime time)
        {
            await _hubContext.Clients.Group(roomId).SendAsync("ReceiveMessage", user, message, time);
        }

        public async Task NotifyAdminNewTicket(int feedbackId)
        {
            // Giả sử Admin luôn join vào một group tên là "AdminGroup" khi đăng nhập
            await _hubContext.Clients.Group("AdminGroup")
                .SendAsync("ReceiveNewTicketNotification", new { id = feedbackId});
        }
    }
}
