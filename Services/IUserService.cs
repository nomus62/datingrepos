// DatingApp.Server/Services/IUserService.cs
using DatingApp.Server.DTOs;
using DatingApp.Server.Models;

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
}