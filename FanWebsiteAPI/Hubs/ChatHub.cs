using Fan_Website;
using FanWebsiteAPI.Infrastructure;
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
        private readonly INotificationService _notificationService;

        public ChatHub(AppDbContext context, UserManager<ApplicationUser> userManager, INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task JoinRoom(int forumId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, RoomKey(forumId));

            // UserImagePath is joined live off the user's current avatar rather than read
            // from ChatMessage's stored column, so history reflects profile picture
            // changes instead of freezing whatever avatar was set at send time.
            var history = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.ForumId == forumId && !m.IsDeleted)
                .Join(_context.Users, m => m.UserId, u => u.Id, (m, u) => new { m, u.ImagePath })
                .OrderByDescending(x => x.m.CreatedAt)
                .Take(50)
                .OrderBy(x => x.m.CreatedAt)
                .Select(x => new
                {
                    x.m.Id,
                    x.m.UserId,
                    x.m.UserName,
                    UserImagePath = x.ImagePath,
                    x.m.Content,
                    x.m.CreatedAt,
                    x.m.ReplyToMessageId,
                    x.m.ReplyToUserName,
                    x.m.ReplyToContent,
                    x.m.IsSystem
                })
                .ToListAsync();

            await Clients.Caller.SendAsync("LoadHistory", history);

            var memberCount = await _context.ChatParticipants.CountAsync(p => p.ForumId == forumId);
            await Clients.Caller.SendAsync("ParticipantCount", memberCount);
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

            var hasJoined = await _context.ChatParticipants
                .AnyAsync(p => p.ForumId == forumId && p.UserId == userId);
            if (!hasJoined) return;

            string? replyToUserName = null;
            string? replyToContent = null;
            if (replyToMessageId.HasValue)
            {
                var parent = await _context.ChatMessages
                    .AsNoTracking()
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
                message.ReplyToContent,
                message.IsSystem
            });

            await NotifyOtherParticipants(forumId, userId, user.UserName ?? userId, content);
        }

        // Notifies everyone who's joined this forum's chat (except the sender) via the
        // same channel used for follows/etc. — an in-app bell entry plus a push notification
        // through the user's stored Expo token, so a message still reaches someone with the
        // app closed. Joining the chat is what opts a user into this (see the "Join to send
        // messages and get notified" prompt client-side).
        private async Task NotifyOtherParticipants(int forumId, string senderId, string senderName, string content)
        {
            var recipientIds = await _context.ChatParticipants
                .AsNoTracking()
                .Where(p => p.ForumId == forumId && p.UserId != senderId)
                .Select(p => p.UserId)
                .ToListAsync();

            if (recipientIds.Count == 0) return;

            var forumName = await _context.Forums
                .AsNoTracking()
                .Where(f => f.ForumId == forumId)
                .Select(f => f.PostTitle)
                .FirstOrDefaultAsync() ?? "a forum";

            var preview = content.Length > 120 ? content[..120] + "…" : content;
            var message = $"{senderName} in {forumName} chat: {preview}";
            var link = $"/forum/{forumId}";

            foreach (var recipientId in recipientIds)
            {
                await _notificationService.CreateAsync(recipientId, message, "chat", link);
            }
        }

        private static string RoomKey(int forumId) => $"forum-{forumId}";
    }
}
