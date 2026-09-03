using Fan_Website;
using FanWebsiteAPI.Models.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FanWebsiteAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatHub(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task JoinRoom(int forumId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, RoomKey(forumId));

            var history = await _context.ChatMessages
                .Where(m => m.ForumId == forumId && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .Take(50)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    m.UserId,
                    m.UserName,
                    m.UserImagePath,
                    m.Content,
                    m.CreatedAt
                })
                .ToListAsync();

            await Clients.Caller.SendAsync("LoadHistory", history);
        }

        public async Task LeaveRoom(int forumId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomKey(forumId));
        }

        public async Task SendMessage(int forumId, string content)
        {
            content = content.Trim();
            if (string.IsNullOrEmpty(content) || content.Length > 500) return;

            var userId = _userManager.GetUserId(Context.User);
            if (userId == null) return;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            var message = new ChatMessage
            {
                ForumId = forumId,
                UserId = userId,
                UserName = user.UserName ?? userId,
                UserImagePath = user.ImagePath,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            await Clients.Group(RoomKey(forumId)).SendAsync("ReceiveMessage", new
            {
                message.Id,
                message.UserId,
                message.UserName,
                message.UserImagePath,
                message.Content,
                message.CreatedAt
            });
        }

        private static string RoomKey(int forumId) => $"forum-{forumId}";
    }
}
