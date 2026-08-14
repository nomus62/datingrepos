// DatingApp.Server/Controllers/SearchController.cs
using DatingApp.Server.DTOs;
using DatingApp.Server.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace DatingApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<SearchController> _logger;

        public SearchController(IUserService userService, ILogger<SearchController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> SearchProfiles(
         [FromQuery] string? gender,
         [FromQuery] int? ageFrom,
         [FromQuery] int? ageTo,
         [FromQuery] string? city,
         [FromQuery] int page = 1,
         [FromQuery] int size = 20)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { message = "Недействительный токен" });

                var filter = new SearchFilterDto
                {
                    UserId = userId,
                    Gender = gender,
                    AgeFrom = ageFrom,
                    AgeTo = ageTo,
                    City = city,
                    Page = page,
                    Size = size
                };

                var profiles = await _userService.SearchProfilesAsync(filter);
                return Ok(profiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка поиска профилей");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }
    }
}