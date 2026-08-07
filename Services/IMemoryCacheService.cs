// DatingApp.Server/Services/IMemoryCacheService.cs
using DatingApp.Server.Models;

namespace DatingApp.Server.Services
{
    public interface IMemoryCacheService
    {
        Task UpdateOnlineStatusAsync(int userId, bool isOnline);
        Task<bool> IsUserOnlineAsync(int userId);
        Task<IEnumerable<int>> GetOnlineUserIdsAsync();
        Task UpdateTypingStatusAsync(int userId, bool isTyping);
        Task<bool> IsUserTypingAsync(int userId);
        Task ClearExpiredEntriesAsync();
    }
}