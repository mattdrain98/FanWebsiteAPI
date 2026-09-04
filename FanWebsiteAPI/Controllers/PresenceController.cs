using FanWebsiteAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FanWebsiteAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PresenceController : ControllerBase
    {
        private readonly PresenceTracker _presence;

        public PresenceController(PresenceTracker presence)
        {
            _presence = presence;
        }

        // GET: api/Presence/online?userIds=a,b,c
        [HttpGet("online")]
        public IActionResult GetOnline([FromQuery] string userIds)
        {
            if (string.IsNullOrWhiteSpace(userIds))
                return Ok(new { onlineUserIds = Array.Empty<string>() });

            var ids = userIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var online = _presence.GetOnline(ids);

            return Ok(new { onlineUserIds = online });
        }
    }
}
