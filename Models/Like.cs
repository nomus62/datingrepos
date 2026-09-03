using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DatingApp.Server.Models;

[Table("Likes")]
public class Like
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("SourceUser")]
    public int SourceUserId { get; set; }
    public virtual User SourceUser { get; set; } = null!;

    [ForeignKey("TargetUser")]
    public int TargetUserId { get; set; }
    public virtual User TargetUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsMutual { get; set; }
}