namespace DatingApp.Server.DTOs;

public class RegisterDto
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public class LoginDto
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class TokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}

public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class SearchFilterDto
{
    public int UserId { get; set; }
    // ❌ SearchText - УДАЛЕН!
    public string? Gender { get; set; }
    // ❌ AgeFrom - УДАЛЕН!
    // ❌ AgeTo - УДАЛЕН!
    public string? City { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
}

public class ProfileDto
{
    public int Id { get; set; }
    public int UserId { get; set; }  // ✅ ДОБАВЛЕН!
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? About { get; set; }
    public List<PhotoDto> Photos { get; set; } = new();
    public bool IsOnline { get; set; }
    public DateTime? LastOnlineAt { get; set; }
}

public class PhotoDto
{
    public int Id { get; set; }
    public string ThumbUrl { get; set; } = string.Empty;
    public string MediumUrl { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public bool IsMain { get; set; }
}

public class UpdateProfileDto
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? About { get; set; }
}

public class DialogDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
    public string UserPhoto { get; set; } = string.Empty;
}

public class LikeDto
{
    public int Id { get; set; }
    public int SourceUserId { get; set; }
    public int TargetUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsMutual { get; set; }
    public ProfileDto? TargetProfile { get; set; }
    public ProfileDto? SourceProfile { get; set; }
}

public class MatchDto
{
    public int UserId { get; set; }
    public ProfileDto Profile { get; set; } = null!;
    public DateTime MatchedAt { get; set; }
}

public class CheckLikeResponse
{
    public bool IsLiked { get; set; }
}

public class SendMessageDto
{
    public int ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class MessageDto
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsRead { get; set; }
    public ProfileDto? SenderProfile { get; set; }
    public ProfileDto? ReceiverProfile { get; set; }
}

public class TypingStatusDto
{
    public int UserId { get; set; }
    public bool IsTyping { get; set; }
    public DateTime Timestamp { get; set; }
}