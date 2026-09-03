using Fan_Website;
using FanWebsiteAPI.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FanWebsiteAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Moderator")]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<ChatHub> _hub;

        public ChatController(AppDbContext context, IHubContext<ChatHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        [HttpDelete("{id}")]
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
    }
}
