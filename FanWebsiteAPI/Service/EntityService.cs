using Microsoft.EntityFrameworkCore;

namespace Fan_Website.Service
{
    public abstract class EntityService<T> where T : class
    {
        protected readonly AppDbContext _context;

        protected EntityService(AppDbContext context)
        {
            _context = context;
        }

        public virtual IQueryable<T> Query() => _context.Set<T>().AsNoTracking();
    }
}
