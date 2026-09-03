using DatingApp.Server.Data;
using DatingApp.Server.DTOs;
using DatingApp.Server.Models;
using DatingApp.Server.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Security.Claims;

namespace DatingApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LikesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IUserService _userService;
    private readonly ILogger<LikesController> _logger;

    public LikesController(
        AppDbContext context,
        IUserService userService,
        ILogger<LikesController> logger)
    {
        _context = context;
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("{userId}")]
    public async Task<IActionResult> LikeUser(int userId)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized(new { message = "Недействительный токен" });

            if (currentUserId == userId)
                return BadRequest(new { message = "Нельзя лайкнуть самого себя" });

            var targetUser = await _context.Users.FindAsync(userId);
            if (targetUser == null)
                return NotFound(new { message = "Пользователь не найден" });

            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.SourceUserId == currentUserId && l.TargetUserId == userId);

            if (existingLike != null)
                return BadRequest(new { message = "Вы уже лайкнули этого пользователя" });

            var mutualLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.SourceUserId == userId && l.TargetUserId == currentUserId);

            var like = new Like
            {
                SourceUserId = currentUserId,
                TargetUserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsMutual = mutualLike != null
            };

            _context.Likes.Add(like);
            await _context.SaveChangesAsync();

            if (mutualLike != null)
            {
                mutualLike.IsMutual = true;
                like.IsMutual = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Матч! {SourceUserId} и {TargetUserId}", currentUserId, userId);
            }

            return Ok(new
            {
                isMutual = like.IsMutual,
                message = like.IsMutual ? "Взаимный лайк! Это матч!" : "Лайк поставлен"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при лайке пользователя {UserId}", userId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> UnlikeUser(int userId)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized(new { message = "Недействительный токен" });

            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.SourceUserId == currentUserId && l.TargetUserId == userId);

            if (like == null)
                return NotFound(new { message = "Лайк не найден" });

            if (like.IsMutual)
            {
                var mutualLike = await _context.Likes
                    .FirstOrDefaultAsync(l => l.SourceUserId == userId && l.TargetUserId == currentUserId);

                if (mutualLike != null)
                    mutualLike.IsMutual = false;
            }

            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Лайк убран" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении лайка пользователя {UserId}", userId);
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpGet("check/{userId}")]
    public async Task<IActionResult> CheckLike(int userId)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized(new { message = "Недействительный токен" });

            var like = await _context.Likes
                .AnyAsync(l => l.SourceUserId == currentUserId && l.TargetUserId == userId);

            return Ok(new { isLiked = like });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка проверки лайка");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

     

    [HttpGet("matches")]
    public async Task<IActionResult> GetMatches()
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized(new { message = "Недействительный токен" });

            var matches = await _context.Likes
                .Where(l => l.SourceUserId == currentUserId && l.IsMutual)
                .Include(l => l.TargetUser)
                .ThenInclude(u => u.Profile)
                .ThenInclude(p => p.Photos)
                .Select(l => new MatchDto
                {
                    UserId = l.TargetUserId,
                    MatchedAt = l.CreatedAt,
                    Profile = new ProfileDto
                    {
                        Id = l.TargetUser.Profile!.Id,
                        UserId = l.TargetUser.Profile.UserId,
                        Name = l.TargetUser.Profile.Name,
                        Age = l.TargetUser.Profile.Age,
                        Gender = l.TargetUser.Profile.Gender,
                        City = l.TargetUser.Profile.City,
                        About = l.TargetUser.Profile.About,
                        IsOnline = false,
                        LastOnlineAt = l.TargetUser.LastOnlineAt,
                        Photos = l.TargetUser.Profile.Photos.Select(p => new PhotoDto
                        {
                            Id = p.Id,
                            ThumbUrl = p.ThumbUrl,
                            MediumUrl = p.MediumUrl,
                            OriginalUrl = p.OriginalUrl,
                            IsMain = p.IsMain
                        }).ToList()
                    }
                })
                .ToListAsync();

            return Ok(matches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения матчей");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }


    [HttpDelete("matches/{userId}")]
    public async Task<IActionResult> DeleteMatch(int userId)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized(new { message = "Недействительный токен" });

            var like1 = await _context.Likes
                .FirstOrDefaultAsync(l => l.SourceUserId == currentUserId && l.TargetUserId == userId);

            var like2 = await _context.Likes
                .FirstOrDefaultAsync(l => l.SourceUserId == userId && l.TargetUserId == currentUserId);

            if (like1 == null && like2 == null)
                return NotFound(new { message = "Матч не найден" });

            if (like1 != null)
                _context.Likes.Remove(like1);

            if (like2 != null)
                _context.Likes.Remove(like2);

            await _context.SaveChangesAsync();

            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                            (m.SenderId == userId && m.ReceiverId == currentUserId))
                .ToListAsync();

            if (messages.Any())
            {
                _context.Messages.RemoveRange(messages);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Матч удалён: {User1} и {User2}", currentUserId, userId);

            return Ok(new { message = "Матч удалён" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка удаления матча");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpGet("sent")]
    public async Task<IActionResult> GetSentLikes()
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized(new { message = "Недействительный токен" });

            var likes = await _context.Likes
                .Where(l => l.SourceUserId == currentUserId)
                .Include(l => l.TargetUser)
                    .ThenInclude(u => u.Profile)
                .Select(l => new
                {
                    l.TargetUserId,
                    l.CreatedAt,
                    l.IsMutual,
                    Profile = new ProfileDto
                    {
                        Id = l.TargetUser.Profile!.Id,
                        UserId = l.TargetUser.Profile.UserId,
                        Name = l.TargetUser.Profile.Name,
                        Age = l.TargetUser.Profile.Age,
                        Gender = l.TargetUser.Profile.Gender,
                        City = l.TargetUser.Profile.City,
                        About = l.TargetUser.Profile.About,
                    }
                })
                .ToListAsync();

            return Ok(likes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения отправленных лайков");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }
}