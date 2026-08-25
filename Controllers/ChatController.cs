using DatingApp.Server.Data;
using DatingApp.Server.Models;
using DatingApp.Server.DTOs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System.Security.Claims;

namespace DatingApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ChatController> _logger;

    public ChatController(AppDbContext context, ILogger<ChatController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("messages/{userId}")]
    public async Task<IActionResult> GetMessages(int userId)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (currentUserId == 0) return Unauthorized();

        var messages = await _context.Messages
            .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                        (m.SenderId == userId && m.ReceiverId == currentUserId))
            .OrderBy(m => m.SentAt)
            .Select(m => new
            {
                m.Id,
                m.SenderId,
                m.ReceiverId,
                m.Content,
                m.SentAt,
                m.IsRead
            })
            .ToListAsync();

        return Ok(messages);
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (currentUserId == 0) return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(new { message = "Сообщение не может быть пустым" });

        var message = new Message
        {
            SenderId = currentUserId,
            ReceiverId = dto.ReceiverId,
            Content = dto.Content,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message.Id,
            message.SenderId,
            message.ReceiverId,
            message.Content,
            message.SentAt,
            message.IsRead
        });
    }

    // ============================================================
    // 4. ПОЛУЧИТЬ СПИСОК ДИАЛОГОВ (GET /api/chat/dialogs)
    // ============================================================
    [HttpGet("dialogs")]
    public async Task<IActionResult> GetDialogs()
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized(new { message = "Недействительный токен" });

            // Находим всех пользователей, с которыми были сообщения
            var dialogUserIds = await _context.Messages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .Select(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            var dialogs = new List<DialogDto>();

            foreach (var userId in dialogUserIds)
            {
                var lastMessage = await _context.Messages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                                (m.SenderId == userId && m.ReceiverId == currentUserId))
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                var user = await _context.Users
                    .Include(u => u.Profile)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null) continue;

                var unreadCount = await _context.Messages
                    .CountAsync(m => m.SenderId == userId && m.ReceiverId == currentUserId && !m.IsRead);

                dialogs.Add(new DialogDto
                {
                    UserId = userId,
                    UserName = user.Profile?.Name ?? user.Login,
                    LastMessage = lastMessage?.Content ?? "",
                    LastMessageTime = lastMessage?.SentAt ?? DateTime.UtcNow,
                    UnreadCount = unreadCount,
                    UserPhoto = user.Profile?.Photos?.FirstOrDefault()?.ThumbUrl ?? ""
                });
            }

            return Ok(dialogs.OrderByDescending(d => d.LastMessageTime));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения диалогов");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    // ============================================================
    // 5. ОТМЕТИТЬ СООБЩЕНИЕ КАК ПРОЧИТАННОЕ (PUT /api/chat/messages/{id}/read)
    // ============================================================
    [HttpPut("messages/{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized(new { message = "Недействительный токен" });

            var message = await _context.Messages.FindAsync(id);
            if (message == null)
                return NotFound(new { message = "Сообщение не найдено" });

            if (message.ReceiverId != currentUserId)
                return Forbid();

            message.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Сообщение отмечено как прочитанное" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отметки сообщения");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }
}

public class SendMessageDto
{
    public int ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
}

