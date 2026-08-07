// DatingApp.Server/DTOs/MessageDTOs.cs
namespace DatingApp.Server.DTOs
{
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
}