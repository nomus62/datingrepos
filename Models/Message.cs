using DatingApp.Server.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Message
{
    [Key]
    public int Id { get; set; }

    public int SenderId { get; set; }
    [ForeignKey(nameof(SenderId))]
    public virtual User Sender { get; set; } = null!;

    public int ReceiverId { get; set; }
    [ForeignKey(nameof(ReceiverId))]
    public virtual User Receiver { get; set; } = null!;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public bool IsRead { get; set; }
}