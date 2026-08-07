// DatingApp.Server/Models/UserProfile.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DatingApp.Server.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int Age { get; set; }

        [Required, MaxLength(20)]
        public string Gender { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? About { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Фотографии (храним пути к файлам)
        public ICollection<Photo> Photos { get; set; } = new List<Photo>();
    }
}