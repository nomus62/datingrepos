using DatingApp.Server.Data;
using DatingApp.Server.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Security.Claims;

namespace DatingApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BlockController : ControllerBase
{
    private readonly AppDbContext _context;
    public BlockController(AppDbContext context) => _context = context;

    private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpPost("{userId}")]
    public async Task<IActionResult> Block(int userId)
    {
        if (CurrentUserId == 0) return Unauthorized();
        if (CurrentUserId == userId) return BadRequest(new { message = "Нельзя заблокировать себя" });

        var exists = await _context.Blocks.AnyAsync(b => b.BlockerId == CurrentUserId && b.BlockedId == userId);
        if (exists) return Ok(new { message = "Уже заблокирован" });

        _context.Blocks.Add(new Block { BlockerId = CurrentUserId, BlockedId = userId });
        await _context.SaveChangesAsync();
        return Ok(new { message = "Пользователь заблокирован" });
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> Unblock(int userId)
    {
        if (CurrentUserId == 0) return Unauthorized();
        var block = await _context.Blocks.FirstOrDefaultAsync(b =>
            b.BlockerId == CurrentUserId && b.BlockedId == userId);
        if (block == null) return NotFound();
        _context.Blocks.Remove(block);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Пользователь разблокирован" });
    }

    [HttpGet("check/{userId}")]
    public async Task<IActionResult> Check(int userId)
    {
        if (CurrentUserId == 0) return Unauthorized();
        var isBlocked = await _context.Blocks.AnyAsync(b =>
            b.BlockerId == CurrentUserId && b.BlockedId == userId);
        return Ok(new { isBlocked });
    }

    [HttpGet("list")]
    public async Task<IActionResult> List()
    {
        if (CurrentUserId == 0) return Unauthorized();
        var ids = await _context.Blocks
            .Where(b => b.BlockerId == CurrentUserId)
            .Select(b => b.BlockedId)
            .ToListAsync();
        return Ok(ids);
    }
}