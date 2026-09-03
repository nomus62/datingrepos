using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DatingApp.Server.Models;

[Table("Photos")]
public class Photo
{
    [Key]
    public int Id { get; set; }

    public int UserProfileId { get; set; }
    public virtual UserProfile UserProfile { get; set; } = null!;

    [Required]
    public string OriginalUrl { get; set; } = string.Empty;
    public string MediumUrl { get; set; } = string.Empty;
    public string ThumbUrl { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}