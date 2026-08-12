// DatingApp.Server/Services/UserService.cs
using DatingApp.Server.Data;
using DatingApp.Server.DTOs;
using DatingApp.Server.Models;

using Microsoft.EntityFrameworkCore;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

using System.Text.RegularExpressions;

namespace DatingApp.Server.Services
{

    public interface IUserService
    {
        Task<UserProfile?> GetProfileAsync(int userId);
        Task<UserProfile?> UpdateProfileAsync(int userId, UpdateProfileDto updateDto);
        Task<List<UserProfile>> SearchProfilesAsync(SearchFilterDto filter);
        Task<bool> UploadPhotoAsync(int userId, Stream fileStream, string fileName, string contentType);
        Task<bool> DeletePhotoAsync(int userId, int photoId);
        Task<bool> SetMainPhotoAsync(int userId, int photoId);
    }
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<UserService> _logger;
        private readonly IMemoryCacheService _cacheService;

        public UserService(
            AppDbContext context,
            IWebHostEnvironment webHostEnvironment,
            ILogger<UserService> logger,
            IMemoryCacheService cacheService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<UserProfile?> GetProfileAsync(int userId)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .Include(p => p.Photos)
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile == null)
                    return null;

                // Добавляем онлайн-статус
                profile.User.IsOnline = await _cacheService.IsUserOnlineAsync(userId);

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка получения профиля пользователя {userId}");
                throw;
            }
        }

        public async Task<UserProfile?> UpdateProfileAsync(int userId, UpdateProfileDto updateDto)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile == null)
                    return null;

                // Обновляем поля
                if (!string.IsNullOrEmpty(updateDto.Name))
                    profile.Name = updateDto.Name;

                if (updateDto.Age > 0 && updateDto.Age < 120)
                    profile.Age = updateDto.Age;

                if (!string.IsNullOrEmpty(updateDto.Gender))
                    profile.Gender = updateDto.Gender;

                if (!string.IsNullOrEmpty(updateDto.City))
                    profile.City = updateDto.City;

                if (updateDto.About != null)
                    profile.About = updateDto.About;

                profile.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка обновления профиля пользователя {userId}");
                throw;
            }
        }

        public async Task<List<UserProfile>> SearchProfilesAsync(SearchFilterDto filter)
        {
            try
            {
                var query = _context.UserProfiles
                    .Include(p => p.Photos)
                    .Include(p => p.User)
                    .AsQueryable();

                // Применяем фильтры
                if (!string.IsNullOrEmpty(filter.Gender))
                    query = query.Where(p => p.Gender == filter.Gender);

                if (filter.AgeFrom.HasValue)
                    query = query.Where(p => p.Age >= filter.AgeFrom.Value);

                if (filter.AgeTo.HasValue)
                    query = query.Where(p => p.Age <= filter.AgeTo.Value);

                if (!string.IsNullOrEmpty(filter.City))
                    query = query.Where(p => p.City.Contains(filter.City));

                // Пагинация
                var page = filter.Page > 0 ? filter.Page : 1;
                var size = filter.Size > 0 && filter.Size <= 100 ? filter.Size : 20;

                var profiles = await query
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToListAsync();

                // Добавляем онлайн-статус
                var onlineUsers = await _cacheService.GetOnlineUserIdsAsync();
                foreach (var profile in profiles)
                {
                    if (profile.User != null)
                    {
                        profile.User.IsOnline = onlineUsers.Contains(profile.UserId);
                    }
                }

                return profiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка поиска профилей");
                throw;
            }
        }

        public async Task<bool> UploadPhotoAsync(int userId, Stream fileStream, string fileName, string contentType)
        {
            try
            {
                // Проверяем размер файла (макс 10MB)
                if (fileStream.Length > 10 * 1024 * 1024)
                    throw new ArgumentException("Файл слишком большой. Максимальный размер - 10MB");

                // Проверяем расширение
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                if (!new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(extension))
                    throw new ArgumentException("Неподдерживаемый формат файла");

                var userProfile = await _context.UserProfiles
                    .Include(p => p.Photos)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (userProfile == null)
                    return false;

                // Проверяем лимит фотографий (макс 5)
                if (userProfile.Photos.Count >= 5)
                    throw new InvalidOperationException("Достигнут лимит фотографий (максимум 5)");

                // Генерируем уникальное имя файла
                var guid = Guid.NewGuid().ToString();
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "photos");

                // Создаем папки для разных размеров
                var originalFolder = Path.Combine(uploadsFolder, "original");
                var mediumFolder = Path.Combine(uploadsFolder, "medium");
                var thumbFolder = Path.Combine(uploadsFolder, "thumb");

                Directory.CreateDirectory(originalFolder);
                Directory.CreateDirectory(mediumFolder);
                Directory.CreateDirectory(thumbFolder);

                // Обработка изображения
                using var image = await Image.LoadAsync(fileStream);

                // Сохраняем оригинал
                var originalPath = Path.Combine(originalFolder, $"{guid}{extension}");
                await image.SaveAsync(originalPath);

                // Создаем среднее изображение (500x500)
                var mediumPath = Path.Combine(mediumFolder, $"{guid}{extension}");
                using (var mediumImage = image.Clone(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(500, 500),
                    Mode = ResizeMode.Max
                })))
                {
                    await mediumImage.SaveAsync(mediumPath);
                }

                // Создаем миниатюру (150x150)
                var thumbPath = Path.Combine(thumbFolder, $"{guid}{extension}");
                using (var thumbImage = image.Clone(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(150, 150),
                    Mode = ResizeMode.Crop
                })))
                {
                    await thumbImage.SaveAsync(thumbPath);
                }

                // Создаем запись в БД
                var photo = new Photo
                {
                    UserProfileId = userProfile.Id,
                    OriginalUrl = $"/photos/original/{guid}{extension}",
                    MediumUrl = $"/photos/medium/{guid}{extension}",
                    ThumbUrl = $"/photos/thumb/{guid}{extension}",
                    IsMain = !userProfile.Photos.Any(p => p.IsMain),
                    UploadedAt = DateTime.UtcNow
                };

                userProfile.Photos.Add(photo);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка загрузки фото для пользователя {userId}");
                throw;
            }
        }

        public async Task<bool> DeletePhotoAsync(int userId, int photoId)
        {
            try
            {
                var photo = await _context.Photos
                    .Include(p => p.UserProfile)
                    .FirstOrDefaultAsync(p => p.Id == photoId && p.UserProfile.UserId == userId);

                if (photo == null)
                    return false;

                // Удаляем файлы
                var webRootPath = _webHostEnvironment.WebRootPath;
                var filePaths = new[]
                {
                    Path.Combine(webRootPath, photo.OriginalUrl.TrimStart('/')),
                    Path.Combine(webRootPath, photo.MediumUrl.TrimStart('/')),
                    Path.Combine(webRootPath, photo.ThumbUrl.TrimStart('/'))
                };

                foreach (var filePath in filePaths)
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }

                // Если удаляем главное фото, назначаем новое
                if (photo.IsMain)
                {
                    var newMain = await _context.Photos
                        .Where(p => p.UserProfileId == photo.UserProfileId && p.Id != photoId)
                        .FirstOrDefaultAsync();

                    if (newMain != null)
                    {
                        newMain.IsMain = true;
                    }
                }

                _context.Photos.Remove(photo);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка удаления фото {photoId} пользователя {userId}");
                throw;
            }
        }

        public async Task<bool> SetMainPhotoAsync(int userId, int photoId)
        {
            try
            {
                var photos = await _context.Photos
                    .Where(p => p.UserProfile.UserId == userId)
                    .ToListAsync();

                var targetPhoto = photos.FirstOrDefault(p => p.Id == photoId);
                if (targetPhoto == null)
                    return false;

                // Снимаем флаг IsMain со всех фото
                foreach (var photo in photos)
                {
                    photo.IsMain = false;
                }

                // Устанавливаем новое главное фото
                targetPhoto.IsMain = true;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка установки главного фото {photoId} пользователя {userId}");
                throw;
            }
        }
    }
}