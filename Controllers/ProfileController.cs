using DatingApp.Server.DTOs;
using DatingApp.Server.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace DatingApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IUserService userService, ILogger<ProfileController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetProfile(int userId)
    {
        try
        {
            var profile = await _userService.GetProfileAsync(userId);
            if (profile == null)
                return NotFound(new { message = "Профиль не найден" });

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения профиля");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            var profile = await _userService.GetProfileAsync(userId);
            if (profile == null)
                return NotFound(new { message = "Профиль не найден" });

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения моего профиля");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateDto)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            var profile = await _userService.UpdateProfileAsync(userId, updateDto);
            if (profile == null)
                return NotFound(new { message = "Профиль не найден" });

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обновления профиля");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpPost("photos")]
    public async Task<IActionResult> UploadPhoto(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Файл не выбран" });

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            using var stream = file.OpenReadStream();
            var success = await _userService.UploadPhotoAsync(userId, stream, file.FileName, file.ContentType);

            if (!success)
                return BadRequest(new { message = "Не удалось загрузить фото" });

            return Ok(new { message = "Фото загружено успешно" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки фото");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpDelete("photos/{photoId}")]
    public async Task<IActionResult> DeletePhoto(int photoId)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            var success = await _userService.DeletePhotoAsync(userId, photoId);
            if (!success)
                return NotFound(new { message = "Фото не найдено" });

            return Ok(new { message = "Фото удалено" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка удаления фото");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }

    [HttpPut("photos/{photoId}/main")]
    public async Task<IActionResult> SetMainPhoto(int photoId)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            var success = await _userService.SetMainPhotoAsync(userId, photoId);
            if (!success)
                return NotFound(new { message = "Фото не найдено" });

            return Ok(new { message = "Главное фото обновлено" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка установки главного фото");
            return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
        }
    }
}