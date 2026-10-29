using System;
using System.Linq;
using System.Threading.Tasks;
using Fan_Website;
using FanWebsiteAPI.Hubs;
using FanWebsiteAPI.Models.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FanWebsiteAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : BaseApiController
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<ChatHub> _hub;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(AppDbContext context, IHubContext<ChatHub> hub, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _hub = hub;
            _userManager = userManager;
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var message = await _context.ChatMessages.FindAsync(id);
            if (message == null) return NotFound();

            message.IsDeleted = true;
            await _context.SaveChangesAsync();

            await _hub.Clients.Group($"forum-{message.ForumId}")
                .SendAsync("MessageDeleted", id);

            return NoContent();
        }

        // POST: api/Chat/{forumId}/join
        [HttpPost("{forumId}/join")]
        public async Task<IActionResult> JoinForumChat(int forumId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var alreadyJoined = await _context.ChatParticipants
                .AnyAsync(p => p.ForumId == forumId && p.UserId == userId);

            if (!alreadyJoined)
            {
                _context.ChatParticipants.Add(new ChatParticipant { ForumId = forumId, UserId = userId });

                var user = await _userManager.FindByIdAsync(userId);
                await PostSystemMessage(forumId, userId, user?.UserName ?? userId, user?.ImagePath,
                    $"{user?.UserName ?? "Someone"} joined the chat");

                var memberCount = await _context.ChatParticipants.CountAsync(p => p.ForumId == forumId);
                await _hub.Clients.Group($"forum-{forumId}").SendAsync("ParticipantCount", memberCount);
            }

            return Ok(new { joined = true });
        }

        // POST: api/Chat/{forumId}/leave
        [HttpPost("{forumId}/leave")]
        public async Task<IActionResult> LeaveForumChat(int forumId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var participant = await _context.ChatParticipants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ForumId == forumId && p.UserId == userId);

            if (participant != null)
            {
                _context.ChatParticipants.Remove(participant);

                var user = await _userManager.FindByIdAsync(userId);
                await PostSystemMessage(forumId, userId, user?.UserName ?? userId, user?.ImagePath,
                    $"{user?.UserName ?? "Someone"} left the chat");

                var memberCount = await _context.ChatParticipants.CountAsync(p => p.ForumId == forumId);
                await _hub.Clients.Group($"forum-{forumId}").SendAsync("ParticipantCount", memberCount);
            }

            return Ok(new { joined = false });
        }

        // GET: api/Chat/{forumId}/joined
        [HttpGet("{forumId}/joined")]
        public async Task<IActionResult> IsJoined(int forumId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var joined = await _context.ChatParticipants
                .AnyAsync(p => p.ForumId == forumId && p.UserId == userId);

            return Ok(new { joined });
        }

        // GET: api/Chat/{forumId}/members
        [HttpGet("{forumId}/members")]
        public async Task<IActionResult> GetMembers(int forumId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            page = ClampPage(page, 1000);

            var query = _context.ChatParticipants
                .AsNoTracking()
                .Where(p => p.ForumId == forumId)
                .Join(_context.Users, p => p.UserId, u => u.Id, (p, u) => new { p.JoinedAt, u.Id, u.UserName, u.ImagePath });

            var totalMembers = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalMembers / (double)pageSize));

            var members = await query
                .OrderBy(x => x.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new { userId = x.Id, userName = x.UserName, userImagePath = x.ImagePath, joinedAt = x.JoinedAt })
                .ToListAsync();

            return Ok(new { members, page, totalPages, totalMembers });
        }

        // GET: api/Chat/my-chats
        [HttpGet("my-chats")]
        public async Task<IActionResult> GetMyChats()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // System notices ("X joined the chat") and deleted messages don't count as
            // activity or unread, and a user's own messages are never unread for them.
            var chats = await _context.ChatParticipants
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Join(_context.Forums,
                    p => p.ForumId,
                    f => f.ForumId,
                    (p, f) => new
                    {
                        forumId = f.ForumId,
                        forumName = f.PostTitle,
                        joinedAt = p.JoinedAt,
                        lastReadAt = p.LastReadAt ?? p.JoinedAt,
                        memberCount = _context.ChatParticipants.Count(x => x.ForumId == f.ForumId),
                        lastMessage = _context.ChatMessages
                            .Where(m => m.ForumId == f.ForumId && !m.IsSystem && !m.IsDeleted)
                            .OrderByDescending(m => m.CreatedAt)
                            .Select(m => new { userName = m.UserName, content = m.Content, createdAt = m.CreatedAt })
                            .FirstOrDefault(),
                        unreadCount = _context.ChatMessages.Count(m =>
                            m.ForumId == f.ForumId &&
                            !m.IsSystem && !m.IsDeleted &&
                            m.UserId != userId &&
                            m.CreatedAt > (p.LastReadAt ?? p.JoinedAt))
                    })
                .ToListAsync();

            // Most recently active chat first; chats with no messages yet fall back to join date.
            var result = chats
                .Select(c => new
                {
                    c.forumId,
                    c.forumName,
                    c.joinedAt,
                    c.memberCount,
                    c.unreadCount,
                    c.lastMessage,
                    lastActivityAt = c.lastMessage?.createdAt ?? c.joinedAt
                })
                .OrderByDescending(c => c.lastActivityAt)
                .ToList();

            return Ok(result);
        }

        // POST: api/Chat/{forumId}/read
        // Marks the chat as read up to now, clearing its unread count in "My chats".
        [HttpPost("{forumId}/read")]
        public async Task<IActionResult> MarkChatRead(int forumId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var updated = await _context.ChatParticipants
                .Where(p => p.ForumId == forumId && p.UserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastReadAt, DateTime.UtcNow));

            if (updated == 0) return NotFound(new { message = "You have not joined this chat." });

            return NoContent();
        }

        // Persists a join/leave notice as a real ChatMessage (IsSystem = true) instead of
        // an ephemeral SignalR-only event, so it survives reconnects and reappears in
        // history via ChatHub.JoinRoom's LoadHistory the same way a real message does.
        // Also saves any other pending changes on _context (e.g. the ChatParticipant
        // add/remove that triggered this), committing both in one round trip.
        private async Task PostSystemMessage(int forumId, string userId, string userName, string? userImagePath, string text)
        {
            var message = new ChatMessage
            {
                ForumId = forumId,
                UserId = userId,
                UserName = userName,
                UserImagePath = userImagePath,
                Content = text,
                CreatedAt = DateTime.UtcNow,
                IsSystem = true
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            await _hub.Clients.Group($"forum-{forumId}").SendAsync("ReceiveMessage", new
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
        }
    }
}
