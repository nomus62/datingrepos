using DatingApp.Server.Data;
using DatingApp.Server.Services;

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
    private readonly IMemoryCacheService _cacheService;
    private readonly IWebHostEnvironment _env;
    private readonly ISupabaseStorageService _storage;

    public AdminController(AppDbContext context, IMemoryCacheService cacheService, IWebHostEnvironment env, ISupabaseStorageService storage)
    {
        _context = context;
        _cacheService = cacheService;
        _env = env;
        _storage = storage;
    }

    private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    private async Task<bool> IsAdminAsync()
        => await _context.Users.AnyAsync(u => u.Id == CurrentUserId && u.IsAdmin);

    // =============== ДАШБОРД ===============
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        if (!await IsAdminAsync()) return Forbid();

        var now = DateTime.UtcNow;
        var dayAgo = now.AddDays(-1);
        var weekAgo = now.AddDays(-7);

        var totalUsers = await _context.Users.CountAsync();
        var newToday = await _context.Users.CountAsync(u => u.CreatedAt >= dayAgo);
        var newWeek = await _context.Users.CountAsync(u => u.CreatedAt >= weekAgo);
        var bannedUsers = await _context.Users.CountAsync(u => u.IsBanned);
        var totalMatches = await _context.Likes.CountAsync(l => l.IsMutual) / 2;
        var totalMessages = await _context.Messages.CountAsync();
        var messagesToday = await _context.Messages.CountAsync(m => m.SentAt >= dayAgo);
        var pendingReports = await _context.Reports.CountAsync(r => r.Status == "Pending");
        var onlineIds = await _cacheService.GetOnlineUserIdsAsync();

        return Ok(new
        {
            totalUsers,
            newToday,
            newWeek,
            bannedUsers,
            totalMatches,
            totalMessages,
            messagesToday,
            pendingReports,
            onlineCount = onlineIds.Count()
        });
    }

    // =============== ЖАЛОБЫ ===============
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
                    .Where(p => p.UserId == r.ReporterId).Select(p => p.Name).FirstOrDefault(),
                ReportedName = _context.UserProfiles
                    .Where(p => p.UserId == r.ReportedId).Select(p => p.Name).FirstOrDefault(),
                ReportsCount = _context.Reports.Count(x => x.ReportedId == r.ReportedId)
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

    [HttpPost("reports/{id}/reject")]
    public async Task<IActionResult> RejectReport(int id)
    {
        if (!await IsAdminAsync()) return Forbid();
        var report = await _context.Reports.FindAsync(id);
        if (report == null) return NotFound();
        report.Status = "Rejected";
        report.ResolvedBy = CurrentUserId;
        report.ResolvedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Жалоба отклонена" });
    }

    // =============== ПОЛЬЗОВАТЕЛИ ===============
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

    [HttpPost("users/{id}/grant-admin")]
    public async Task<IActionResult> GrantAdmin(int id)
    {
        if (!await IsAdminAsync()) return Forbid();
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.IsAdmin = true;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Права админа выданы" });
    }

    [HttpPost("users/{id}/revoke-admin")]
    public async Task<IActionResult> RevokeAdmin(int id)
    {
        if (!await IsAdminAsync()) return Forbid();
        if (id == CurrentUserId) return BadRequest(new { message = "Нельзя снять с себя" });
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.IsAdmin = false;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Права админа сняты" });
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (!await IsAdminAsync()) return Forbid();
        if (id == CurrentUserId) return BadRequest(new { message = "Нельзя удалить себя" });

        var user = await _context.Users
            .Include(u => u.Profile).ThenInclude(p => p.Photos)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        // Удаляем файлы фото
        if (user.Profile?.Photos != null)
        {
            foreach (var photo in user.Profile.Photos)
            {
                foreach (var url in new[] { photo.OriginalUrl, photo.MediumUrl, photo.ThumbUrl })
                {
                    var path = Path.Combine(_env.WebRootPath, url.TrimStart('/'));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
            }
        }

        // Каскадное удаление связанных записей
        _context.Likes.RemoveRange(await _context.Likes
            .Where(l => l.SourceUserId == id || l.TargetUserId == id).ToListAsync());
        _context.Messages.RemoveRange(await _context.Messages
            .Where(m => m.SenderId == id || m.ReceiverId == id).ToListAsync());
        _context.Blocks.RemoveRange(await _context.Blocks
            .Where(b => b.BlockerId == id || b.BlockedId == id).ToListAsync());
        _context.Reports.RemoveRange(await _context.Reports
            .Where(r => r.ReporterId == id || r.ReportedId == id).ToListAsync());

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Пользователь удалён" });
    }

    [HttpPost("test-storage")]
    public async Task<IActionResult> TestStorage(IFormFile file)
    {
        if (!await IsAdminAsync()) return Forbid();
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Файл не выбран" });

        try
        {
            using var stream = file.OpenReadStream();
            var publicUrl = await _storage.UploadAsync(stream, file.FileName, file.ContentType);

            return Ok(new
            {
                message = "Файл загружен в Supabase Storage",
                url = publicUrl,
                size = file.Length,
                name = file.FileName
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    public class BanDto { public string? Reason { get; set; } }
}