// DatingApp.Server/Services/ITokenService.cs
using DatingApp.Server.DTOs;
using DatingApp.Server.Models;

namespace DatingApp.Server.Services
{
    public interface ITokenService
    {
        Task<string> GenerateAccessToken(User user);
        string GenerateRefreshToken();
        Task<TokenDto> CreateTokens(User user);
    }
}