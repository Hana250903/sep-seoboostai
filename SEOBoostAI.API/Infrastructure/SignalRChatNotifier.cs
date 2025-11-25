using Microsoft.AspNetCore.SignalR;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Infrastructure
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
    }
}
