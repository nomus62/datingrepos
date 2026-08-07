// DatingApp.Server/DTOs/LikeDTOs.cs
namespace DatingApp.Server.DTOs
{
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
}