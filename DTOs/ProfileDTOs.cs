// DatingApp.Server/DTOs/ProfileDTOs.cs
namespace DatingApp.Server.DTOs
{
    public class ProfileDto
    {
        public int Id { get; set; }
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
}