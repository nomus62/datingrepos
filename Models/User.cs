using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DatingApp.Server.Models;

[Table("Users")]
public class User
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;
    public DateTime? RefreshTokenExpiry { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsOnline { get; set; }
    public DateTime? LastOnlineAt { get; set; }

    public UserProfile? Profile { get; set; }
    public ICollection<Like> SentLikes { get; set; } = new List<Like>();
    public ICollection<Like> ReceivedLikes { get; set; } = new List<Like>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
}