using Microsoft.AspNetCore.SignalR;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API.Infrastructure
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task SendMessageToRoom(string feedbackIdStr, int senderId, string senderName, string message)
        {
            if (int.TryParse(feedbackIdStr, out int feedbackId))
            {
                await _chatService.SendMessageAsync(feedbackId, senderId, senderName, message);
            }
        }

        public async Task JoinChatRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        }
    }
}
