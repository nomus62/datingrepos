using DatingApp.Server.Data;
using DatingApp.Server.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace DatingApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly AppDbContext _context;
    public ReportController(AppDbContext context) => _context = context;

    private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    public class ReportDto
    {
        public int ReportedId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReportDto dto)
    {
        if (CurrentUserId == 0) return Unauthorized();
        if (CurrentUserId == dto.ReportedId)
            return BadRequest(new { message = "Нельзя пожаловаться на себя" });
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest(new { message = "Укажите причину" });
        if (dto.Reason.Length > 500)
            return BadRequest(new { message = "Причина слишком длинная (макс. 500)" });

        _context.Reports.Add(new Report
        {
            ReporterId = CurrentUserId,
            ReportedId = dto.ReportedId,
            Reason = dto.Reason
        });
        await _context.SaveChangesAsync();
        return Ok(new { message = "Жалоба отправлена" });
    }
}