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
                    m.CreatedAt,
                    m.ReplyToMessageId,
                    m.ReplyToUserName,
                    m.ReplyToContent
                })
                .ToListAsync();

            await Clients.Caller.SendAsync("LoadHistory", history);
        }

        public async Task LeaveRoom(int forumId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomKey(forumId));
        }

        // Ephemeral — nothing persisted. Username comes straight off the JWT claim
        // so this never touches the database, safe to call on every keystroke.
        public Task Typing(int forumId)
        {
            var userId = _userManager.GetUserId(Context.User);
            var userName = Context.User?.Identity?.Name;
            if (userId == null || userName == null) return Task.CompletedTask;

            return Clients.OthersInGroup(RoomKey(forumId)).SendAsync("UserTyping", new { userId, userName });
        }

        public async Task SendMessage(int forumId, string content, int? replyToMessageId = null)
        {
            content = content.Trim();
            if (string.IsNullOrEmpty(content) || content.Length > 500) return;

            var userId = _userManager.GetUserId(Context.User);
            if (userId == null) return;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            string? replyToUserName = null;
            string? replyToContent = null;
            if (replyToMessageId.HasValue)
            {
                var parent = await _context.ChatMessages
                    .Where(m => m.Id == replyToMessageId.Value && m.ForumId == forumId)
                    .FirstOrDefaultAsync();
                if (parent != null)
                {
                    replyToUserName = parent.UserName;
                    replyToContent = parent.Content.Length > 120 ? parent.Content[..120] : parent.Content;
                }
            }

            var message = new ChatMessage
            {
                ForumId = forumId,
                UserId = userId,
                UserName = user.UserName ?? userId,
                UserImagePath = user.ImagePath,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                ReplyToMessageId = replyToUserName != null ? replyToMessageId : null,
                ReplyToUserName = replyToUserName,
                ReplyToContent = replyToContent
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
                message.CreatedAt,
                message.ReplyToMessageId,
                message.ReplyToUserName,
                message.ReplyToContent
            });
        }

        private static string RoomKey(int forumId) => $"forum-{forumId}";
    }
}
