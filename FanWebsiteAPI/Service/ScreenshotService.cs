using Fan_Website.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fan_Website.Service
{
    public class ScreenshotService : IScreenshot
    {
        private readonly AppDbContext _context;

        public ScreenshotService(AppDbContext context)
        {
            _context = context;
        }

        public async Task Add(Screenshot screenshot)
        {
            _context.Add(screenshot);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var screenshot = await GetById(id);

            if (screenshot != null)
            {
                _context.Remove(screenshot);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Screenshot>> GetAll()
        {
            return await _context.Screenshots.Include(screenshot => screenshot.User).ToListAsync(); 
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllUsers()
        {
            return await _context.ApplicationUsers.ToListAsync();
        }

        public async Task<Screenshot?> GetById(int id)
        {
            return await _context.Screenshots
                .Include(screenshot => screenshot.User)
                .FirstOrDefaultAsync(screenshot => screenshot.ScreenshotId == id);
        }

        public async Task<IEnumerable<Screenshot>> GetLatestScreenshots(int n)
        {
            return await _context.Screenshots.OrderByDescending(screenshot => screenshot.UpdatedOn).Take(n).ToListAsync();

        }

        public async Task<ApplicationUser?> GetUserById(string id)
        {
            return await _context.Users.FirstOrDefaultAsync(user => user.Id == id);
        }

        public async Task SetScreenshotImage(int id, Uri uri)
        {
            var screenshot = await GetById(id);

            if (screenshot != null)
            {
                screenshot.ImagePath = uri.AbsoluteUri;
                _context.Update(screenshot);
                await _context.SaveChangesAsync();
            }

        }
    }
}
