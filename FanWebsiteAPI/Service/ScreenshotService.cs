using Fan_Website.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fan_Website.Service
{
    public class ScreenshotService : IScreenshot
    {
        private readonly AppDbContext context; 

        public ScreenshotService(AppDbContext ctx)
        {
            context = ctx; 
        }

        public async Task Add(Screenshot screenshot)
        {
            context.Add(screenshot);
            await context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var screenshot = await GetById(id);

            if (screenshot != null)
            {
                context.Remove(screenshot);
                await context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Screenshot>> GetAll()
        {
            return await context.Screenshots.Include(screenshot => screenshot.User).ToListAsync(); 
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllUsers()
        {
            return await context.ApplicationUsers.ToListAsync();
        }

        public async Task<Screenshot?> GetById(int id)
        {
            return await context.Screenshots
                .Include(screenshot => screenshot.User)
                .FirstOrDefaultAsync(screenshot => screenshot.ScreenshotId == id);
        }

        public async Task<IEnumerable<Screenshot>> GetLatestScreenshots(int n)
        {
            return await context.Screenshots.OrderByDescending(screenshot => screenshot.UpdatedOn).Take(n).ToListAsync();

        }

        public async Task<ApplicationUser?> GetUserById(string id)
        {
            return await context.Users.FirstOrDefaultAsync(user => user.Id == id);
        }

        public async Task SetScreenshotImage(int id, Uri uri)
        {
            var screenshot = await GetById(id);

            if (screenshot != null)
            {
                screenshot.ImagePath = uri.AbsoluteUri;
                context.Update(screenshot);
                await context.SaveChangesAsync();
            }

        }
    }
}
