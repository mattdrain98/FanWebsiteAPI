using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using Fan_Website;
using FanWebsiteAPI.Infrastructure;
using FanWebsiteAPI.Models.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FanWebsiteAPI.Hubs
{
    public record RoomMemberInfo(string UserId, string? UserName, string? UserImagePath);

    [Authorize]
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, int> _connectionRoom = new();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, RoomMemberInfo>> _roomMembers = new();

        // One token-bucket limiter per connection, created lazily on first Typing
        // call. Caps bursts at 2, then refills 1 token/sec — enforces this server-
        // side since the client's own debounce is just UX, not a guarantee (a
        // modified client could call Typing on every keystroke otherwise).
        private static readonly ConcurrentDictionary<string, TokenBucketRateLimiter> _typingLimiters = new();

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
            var roomKey = RoomKey(forumId);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomKey);
            _connectionRoom[Context.ConnectionId] = forumId;

            var userId = _userManager.GetUserId(Context.User)!;
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var members = _roomMembers.GetOrAdd(roomKey, _ => new ConcurrentDictionary<string, RoomMemberInfo>());
                members[userId] = new RoomMemberInfo(userId, user.UserName, user.ImagePath);
                await Clients.Group(roomKey).SendAsync("RoomMembersUpdated", members.Values.ToList());
            }

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
            var roomKey = RoomKey(forumId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomKey);
            _connectionRoom.TryRemove(Context.ConnectionId, out _);

            var userId = _userManager.GetUserId(Context.User);
            if (userId != null && _roomMembers.TryGetValue(roomKey, out var members))
            {
                members.TryRemove(userId, out _);
                await Clients.Group(roomKey).SendAsync("RoomMembersUpdated", members.Values.ToList());
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_typingLimiters.TryRemove(Context.ConnectionId, out var limiter))
                limiter.Dispose();

            if (_connectionRoom.TryRemove(Context.ConnectionId, out var forumId))
            {
                var roomKey = RoomKey(forumId);
                var userId = _userManager.GetUserId(Context.User);
                if (userId != null && _roomMembers.TryGetValue(roomKey, out var members))
                {
                    members.TryRemove(userId, out _);
                    await Clients.Group(roomKey).SendAsync("RoomMembersUpdated", members.Values.ToList());
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Ephemeral — nothing persisted. Username comes straight off the JWT claim
        // so this never touches the database, safe to call on every keystroke —
        // rate-limited per connection below so "every keystroke" can't translate
        // into a broadcast storm.
        public Task Typing(int forumId)
        {
            var userId = _userManager.GetUserId(Context.User);
            var userName = Context.User?.Identity?.Name;
            if (userId == null || userName == null) return Task.CompletedTask;

            var limiter = _typingLimiters.GetOrAdd(Context.ConnectionId, _ => CreateTypingRateLimiter());
            using var lease = limiter.AttemptAcquire();
            if (!lease.IsAcquired) return Task.CompletedTask;

            return Clients.OthersInGroup(RoomKey(forumId)).SendAsync("UserTyping", new { userId, userName });
        }

        private static TokenBucketRateLimiter CreateTypingRateLimiter() => new(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 2,
            TokensPerPeriod = 1,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            AutoReplenishment = true,
            QueueLimit = 0
        });

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
