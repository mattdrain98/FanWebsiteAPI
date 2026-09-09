using Fan_Website;
using FanWebsiteAPI.DTOs.Reports;
using FanWebsiteAPI.Models.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FanWebsiteAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto dto)
        {
            var reporterId = _userManager.GetUserId(User);
            if (reporterId == null) return Unauthorized();

            if (reporterId == dto.TargetUserId)
                return BadRequest(new { message = "You cannot report yourself." });

            try
            {
                var query = _context.Reports.Where(r =>
                    r.ReporterId == reporterId &&
                    r.TargetUserId == dto.TargetUserId &&
                    r.ContentType == dto.ContentType &&
                    r.Status == ReportStatus.Pending);

                query = dto.ContentId.HasValue
                    ? query.Where(r => r.ContentId == dto.ContentId.Value)
                    : query.Where(r => r.ContentId == null);

                if (await query.AnyAsync())
                    return BadRequest(new { message = "You have already reported this content." });

                var report = new Report
                {
                    ReporterId = reporterId,
                    TargetUserId = dto.TargetUserId,
                    ContentType = dto.ContentType,
                    ContentId = dto.ContentId,
                    Reason = dto.Reason,
                    Details = dto.Details,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Report submitted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> GetReports(
            [FromQuery] string status = "Pending",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var statusEnum = Enum.TryParse<ReportStatus>(status, true, out var parsed)
                ? parsed
                : ReportStatus.Pending;

            var query = _context.Reports
                .AsNoTracking()
                .Include(r => r.Reporter)
                .Include(r => r.TargetUser)
                .Include(r => r.ReviewedBy)
                .Where(r => r.Status == statusEnum)
                .OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();
            var reports = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReportDto
                {
                    Id = r.Id,
                    ReporterId = r.ReporterId,
                    ReporterName = r.Reporter.UserName ?? string.Empty,
                    TargetUserId = r.TargetUserId,
                    TargetUserName = r.TargetUser.UserName ?? string.Empty,
                    ContentType = r.ContentType.ToString(),
                    ContentId = r.ContentId,
                    Reason = r.Reason,
                    Details = r.Details,
                    Status = r.Status.ToString(),
                    CreatedAt = r.CreatedAt,
                    ReviewNote = r.ReviewNote,
                    ReviewedAt = r.ReviewedAt,
                    ReviewedByName = r.ReviewedBy != null ? r.ReviewedBy.UserName : null
                })
                .ToListAsync();

            // Populate chat message previews in a single follow-up query
            var chatIds = reports
                .Where(r => r.ContentType == "ChatMessage" && r.ContentId.HasValue)
                .Select(r => r.ContentId!.Value)
                .ToList();

            if (chatIds.Count > 0)
            {
                var chatContents = await _context.ChatMessages
                    .Where(m => chatIds.Contains(m.Id))
                    .Select(m => new { m.Id, m.Content })
                    .ToDictionaryAsync(m => m.Id, m => m.Content);

                foreach (var r in reports.Where(r => r.ContentType == "ChatMessage" && r.ContentId.HasValue))
                    r.ContentPreview = chatContents.GetValueOrDefault(r.ContentId!.Value);
            }

            return Ok(new { reports, total, page, totalPages = (int)Math.Ceiling((double)total / pageSize) });
        }

        [HttpPost("{id}/review")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> ReviewReport(int id, [FromBody] ReviewReportDto dto)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            var reviewerId = _userManager.GetUserId(User);
            report.Status = dto.Action.ToLower() == "dismissed" ? ReportStatus.Dismissed : ReportStatus.Reviewed;
            report.ReviewNote = dto.Note;
            report.ReviewedAt = DateTime.UtcNow;
            report.ReviewedById = reviewerId;

            await _context.SaveChangesAsync();
            return Ok(new { status = report.Status.ToString() });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            _context.Reports.Remove(report);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
