// DatingApp.Server/Services/MemoryCacheService.cs
using System.Collections.Concurrent;

using DatingApp.Server.Models;

using Microsoft.Extensions.Caching.Memory;

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
	
    public class MemoryCacheService : IMemoryCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly ConcurrentDictionary<int, DateTime> _onlineUsers = new();
        private readonly ConcurrentDictionary<int, DateTime> _typingUsers = new();
        private readonly TimeSpan _onlineTimeout = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _typingTimeout = TimeSpan.FromSeconds(3);
        private readonly Timer _cleanupTimer;

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
            _cleanupTimer = new Timer(CleanupExpired, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        public Task UpdateOnlineStatusAsync(int userId, bool isOnline)
        {
            if (isOnline)
            {
                _onlineUsers[userId] = DateTime.UtcNow;
            }
            else
            {
                _onlineUsers.TryRemove(userId, out _);
            }
            return Task.CompletedTask;
        }

        public Task<bool> IsUserOnlineAsync(int userId)
        {
            return Task.FromResult(_onlineUsers.TryGetValue(userId, out var lastSeen) &&
                                   DateTime.UtcNow - lastSeen < _onlineTimeout);
        }

        public Task<IEnumerable<int>> GetOnlineUserIdsAsync()
        {
            var now = DateTime.UtcNow;
            var onlineIds = _onlineUsers
                .Where(kvp => now - kvp.Value < _onlineTimeout)
                .Select(kvp => kvp.Key)
                .ToList();
            return Task.FromResult<IEnumerable<int>>(onlineIds);
        }

        public Task UpdateTypingStatusAsync(int userId, bool isTyping)
        {
            if (isTyping)
            {
                _typingUsers[userId] = DateTime.UtcNow;
            }
            else
            {
                _typingUsers.TryRemove(userId, out _);
            }
            return Task.CompletedTask;
        }

        public Task<bool> IsUserTypingAsync(int userId)
        {
            return Task.FromResult(_typingUsers.TryGetValue(userId, out var lastTyping) &&
                                   DateTime.UtcNow - lastTyping < _typingTimeout);
        }

        public Task ClearExpiredEntriesAsync()
        {
            CleanupExpired(null);
            return Task.CompletedTask;
        }

        private void CleanupExpired(object? state)
        {
            try
            {
                var now = DateTime.UtcNow;

                var expiredOnline = _onlineUsers
                    .Where(kvp => now - kvp.Value >= _onlineTimeout)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var userId in expiredOnline)
                {
                    _onlineUsers.TryRemove(userId, out _);
                }

                var expiredTyping = _typingUsers
                    .Where(kvp => now - kvp.Value >= _typingTimeout)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var userId in expiredTyping)
                {
                    _typingUsers.TryRemove(userId, out _);
                }

                if (expiredOnline.Any() || expiredTyping.Any())
                {
                    _logger.LogInformation("Очищено {OnlineCount} просроченных онлайн-статусов и {TypingCount} статусов печатания",
                        expiredOnline.Count, expiredTyping.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке кэша");
            }
        }
    }
}