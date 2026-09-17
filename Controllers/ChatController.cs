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
public class ChatController : ControllerBase
{
    private readonly AppDbContext _context;

    private readonly IMemoryCacheService _cacheService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(AppDbContext context, IMemoryCacheService cacheService, ILogger<ChatController> logger)
    {
        _context = context;
        _cacheService = cacheService;        // ← добавить
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
                m.ReadAt,
                m.IsRead,
                m.ReplyToMessageId,
                ReplyToContent = m.ReplyToMessageId == null ? null :
        _context.Messages.Where(x => x.Id == m.ReplyToMessageId)
            .Select(x => x.Content).FirstOrDefault()
            })
            .ToListAsync();

        return Ok(messages);
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized(new { message = "Недействительный токен" });

            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { message = "Сообщение не может быть пустым" });

            if (dto.ReceiverId == currentUserId)
                return BadRequest(new { message = "Нельзя писать самому себе" });

            // Проверка: существует ли получатель
            var receiverExists = await _context.Users.AnyAsync(u => u.Id == dto.ReceiverId);
            if (!receiverExists)
                return NotFound(new { message = "Получатель не найден" });

            // Проверка: есть ли взаимный лайк (матч)
            var isMatch = await _context.Likes.AnyAsync(l =>
                l.SourceUserId == currentUserId &&
                l.TargetUserId == dto.ReceiverId &&
                l.IsMutual);

            if (!isMatch)
                return BadRequest(new { message = "Нужен взаимный лайк, чтобы писать" });

            var isBlocked = await _context.Blocks.AnyAsync(b =>
                (b.BlockerId == currentUserId && b.BlockedId == dto.ReceiverId) ||
                (b.BlockerId == dto.ReceiverId && b.BlockedId == currentUserId));

            if (isBlocked)
                return BadRequest(new { message = "Общение заблокировано" });

            var message = new Message
            {
                SenderId = currentUserId,
                ReceiverId = dto.ReceiverId,
                Content = dto.Content,
                SentAt = DateTime.UtcNow,
                ReplyToMessageId = dto.ReplyToMessageId,
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
                message.ReplyToMessageId,
                message.IsRead
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке сообщения");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpGet("dialogs")]
    public async Task<IActionResult> GetDialogs()
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized(new { message = "Недействительный токен" });

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
                        .ThenInclude(p => p.Photos)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null) continue;

                var unreadCount = await _context.Messages
                    .CountAsync(m => m.SenderId == userId && m.ReceiverId == currentUserId && !m.IsRead);

                var isOnline = await _cacheService.IsUserOnlineAsync(userId);

                dialogs.Add(new DialogDto
                {
                    UserId = userId,
                    UserName = user.Profile?.Name ?? user.Login,
                    LastMessage = lastMessage?.Content ?? "",
                    LastMessageTime = lastMessage?.SentAt ?? DateTime.UtcNow,
                    UnreadCount = unreadCount,
                    UserPhoto = user.Profile?.Photos?.FirstOrDefault()?.ThumbUrl ?? "",
                    IsOnline = isOnline           // ← добавить
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

    //add endpoints
    [HttpPost("typing/{userId}")]
    public async Task<IActionResult> SetTyping(int userId)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (currentUserId == 0) return Unauthorized();

        await _cacheService.UpdateTypingStatusAsync(currentUserId, true);
        return Ok();
    }

    [HttpGet("typing/{userId}")]
    public async Task<IActionResult> IsTyping(int userId)
    {
        var isTyping = await _cacheService.IsUserTypingAsync(userId);
        return Ok(new { isTyping });
    }

    [HttpDelete("messages/{id}")]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0) return Unauthorized();

            var message = await _context.Messages.FindAsync(id);
            if (message == null) return NotFound(new { message = "Сообщение не найдено" });

            // Удалять можно только свои сообщения
            if (message.SenderId != currentUserId)
                return Forbid();

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Сообщение удалено" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка удаления сообщения");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpGet("online/{userId}")]
    public async Task<IActionResult> IsUserOnline(int userId)
    {
        var online = await _cacheService.IsUserOnlineAsync(userId);
        return Ok(new { isOnline = online });
    }
}