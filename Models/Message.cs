using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DatingApp.Server.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int SenderId { get; set; }
        public virtual User Sender { get; set; } = null!;

        [ForeignKey("User")]
        public int ReceiverId { get; set; }
        public virtual User Receiver { get; set; } = null!;

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
        public bool IsRead { get; set; }
    }
}