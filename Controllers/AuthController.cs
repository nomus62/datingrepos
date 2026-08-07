// DatingApp.Server/Controllers/AuthController.cs
using BCrypt.Net;

using DatingApp.Server.Data;
using DatingApp.Server.DTOs;
using DatingApp.Server.Models;
using DatingApp.Server.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Security.Claims;

namespace DatingApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IMemoryCacheService _cacheService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context,
            ITokenService tokenService,
            IMemoryCacheService cacheService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _cacheService = cacheService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                // Валидация
                if (registerDto.Password != registerDto.ConfirmPassword)
                {
                    return BadRequest(new { message = "Пароли не совпадают" });
                }

                if (await _context.Users.AnyAsync(u => u.Login == registerDto.Login))
                {
                    return BadRequest(new { message = "Пользователь с таким логином уже существует" });
                }

                // Создание пользователя
                var user = new User
                {
                    Login = registerDto.Login,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                    CreatedAt = DateTime.UtcNow
                };

                // Создание профиля
                var profile = new UserProfile
                {
                    User = user,
                    Name = registerDto.Name,
                    Age = registerDto.Age,
                    Gender = registerDto.Gender,
                    City = registerDto.City,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Users.AddAsync(user);
                await _context.UserProfiles.AddAsync(profile);
                await _context.SaveChangesAsync();

                // Генерация токенов
                var tokens = await _tokenService.CreateTokens(user);
                user.RefreshToken = tokens.RefreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
                await _context.SaveChangesAsync();

                return Ok(new { tokens });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при регистрации пользователя");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Profile)
                    .FirstOrDefaultAsync(u => u.Login == loginDto.Login);

                if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    return Unauthorized(new { message = "Неверный логин или пароль" });
                }

                // Обновление онлайн-статуса
                await _cacheService.UpdateOnlineStatusAsync(user.Id, true);

                // Генерация токенов
                var tokens = await _tokenService.CreateTokens(user);
                user.RefreshToken = tokens.RefreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
                user.LastOnlineAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { tokens });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе пользователя");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.RefreshToken == refreshTokenDto.RefreshToken);

                if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                {
                    return Unauthorized(new { message = "Недействительный или просроченный refresh-токен" });
                }

                var tokens = await _tokenService.CreateTokens(user);
                user.RefreshToken = tokens.RefreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
                await _context.SaveChangesAsync();

                return Ok(new { tokens });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении токена");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (userId > 0)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.RefreshToken = string.Empty;
                        user.RefreshTokenExpiry = null;
                        await _context.SaveChangesAsync();
                    }

                    await _cacheService.UpdateOnlineStatusAsync(userId, false);
                }

                return Ok(new { message = "Выход выполнен успешно" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выходе пользователя");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }
    }
}