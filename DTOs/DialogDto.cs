namespace DatingApp.Server.DTOs;

public class DialogDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
    public string UserPhoto { get; set; } = string.Empty;
}