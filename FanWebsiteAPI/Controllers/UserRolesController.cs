using Fan_Website;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FanWebsiteAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserRolesController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserRolesController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: api/userroles/{userId}
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Ok(new List<string>());
            var roles = await _userManager.GetRolesAsync(user);
            return Ok(roles);
        }
    }
}
