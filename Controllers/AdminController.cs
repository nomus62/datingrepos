using DatingApp.Server.Data;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Security.Claims;

namespace DatingApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    public AdminController(AppDbContext context) => _context = context;

    private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    private async Task<bool> IsAdminAsync()
        => await _context.Users.AnyAsync(u => u.Id == CurrentUserId && u.IsAdmin);

    // ===== Жалобы =====
    [HttpGet("reports")]
    public async Task<IActionResult> GetReports()
    {
        if (!await IsAdminAsync()) return Forbid();

        var reports = await _context.Reports
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.ReporterId,
                r.ReportedId,
                r.Reason,
                r.CreatedAt,
                r.Status,
                ReporterName = _context.UserProfiles
                    .Where(p => p.UserId == r.ReporterId)
                    .Select(p => p.Name).FirstOrDefault(),
                ReportedName = _context.UserProfiles
                    .Where(p => p.UserId == r.ReportedId)
                    .Select(p => p.Name).FirstOrDefault()
            })
            .ToListAsync();

        return Ok(reports);
    }

    [HttpPost("reports/{id}/resolve")]
    public async Task<IActionResult> ResolveReport(int id)
    {
        if (!await IsAdminAsync()) return Forbid();
        var report = await _context.Reports.FindAsync(id);
        if (report == null) return NotFound();
        report.Status = "Resolved";
        report.ResolvedBy = CurrentUserId;
        report.ResolvedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Жалоба обработана" });
    }

    // ===== Пользователи =====
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        if (!await IsAdminAsync()) return Forbid();

        var users = await _context.Users
            .Include(u => u.Profile)
            .Select(u => new
            {
                u.Id,
                u.Login,
                u.IsAdmin,
                u.IsBanned,
                u.BanReason,
                u.LastOnlineAt,
                Name = u.Profile != null ? u.Profile.Name : "",
                City = u.Profile != null ? u.Profile.City : ""
            })
            .OrderByDescending(u => u.LastOnlineAt)
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("users/{id}/ban")]
    public async Task<IActionResult> Ban(int id, [FromBody] BanDto dto)
    {
        if (!await IsAdminAsync()) return Forbid();
        if (id == CurrentUserId) return BadRequest(new { message = "Нельзя забанить себя" });

        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.IsBanned = true;
        user.BanReason = dto.Reason ?? "Нарушение правил";
        await _context.SaveChangesAsync();
        return Ok(new { message = "Пользователь забанен" });
    }

    [HttpPost("users/{id}/unban")]
    public async Task<IActionResult> Unban(int id)
    {
        if (!await IsAdminAsync()) return Forbid();
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.IsBanned = false;
        user.BanReason = null;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Пользователь разбанен" });
    }

    public class BanDto { public string? Reason { get; set; } }
}