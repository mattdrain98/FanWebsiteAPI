using Microsoft.AspNetCore.Mvc;

namespace FanWebsiteAPI.Controllers
{
    public abstract class BaseApiController : ControllerBase
    {
        protected bool CanModerate => User.IsInRole("Admin") || User.IsInRole("Moderator");

        protected static int ClampPage(int page, int maxPage = 100) => Math.Clamp(page, 1, maxPage);

        protected static int ClampPageSize(int pageSize, int maxPageSize = 100) => Math.Clamp(pageSize, 1, maxPageSize);
    }
}
