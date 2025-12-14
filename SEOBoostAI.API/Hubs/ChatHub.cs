using Microsoft.AspNetCore.SignalR;
using SEOBoostAI.Service.Services;
using SEOBoostAI.Service.Services.Interfaces;
using System.Security.Claims;

namespace SEOBoostAI.API.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ISpamProtectionService _spamProtectionService;

        public ChatHub(IChatService chatService, ISpamProtectionService spamProtectionService)
        {
            _chatService = chatService;
            _spamProtectionService = spamProtectionService;
        }

        public async Task SendMessageToRoom(string feedbackIdStr, string message)
        {
            var userIdClaim = Context.User?.FindFirstValue("user_ID");
            var userName = Context.User?.FindFirstValue("fullname") ?? "Anonymous";

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int senderId))
            {
                // Nếu chưa đăng nhập hoặc lỗi Token
                await Clients.Caller.SendAsync("ReceiveError", "Unauthorized: Bạn chưa đăng nhập.");
                return;
            }

            if (_spamProtectionService.IsUserSpamming(userIdClaim))
            {
                // Gửi thông báo lỗi về RIÊNG người gọi (Caller)
                await Clients.Caller.SendAsync("ReceiveError", "Bạn nhắn quá nhanh! Vui lòng chờ giây lát.");
                return;
            }

            if (int.TryParse(feedbackIdStr, out int feedbackId))
            {
                await _chatService.SendMessageAsync(feedbackId, senderId, userName, message);
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveError", "Phòng chat không hợp lệ.");
            }
        }

        public async Task JoinChatRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        }

        public async Task NotifyTicketStatusChanged(string feedbackId, string newStatus)
        {
            await Clients.Group(feedbackId).SendAsync("ReceiveTicketStatusChange", newStatus);
        }

        //public override async Task OnConnectedAsync()
        //{
        //    var role = Context.User.FindFirstValue("role");
        //    if (role == "Admin")
        //    {
        //        await Groups.AddToGroupAsync(Context.ConnectionId, "AdminGroup");
        //    }
        //    await base.OnConnectedAsync();
        //}
    }
}
