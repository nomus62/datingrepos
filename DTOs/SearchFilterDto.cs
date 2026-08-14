// DatingApp.Server/DTOs/SearchFilterDto.cs
namespace DatingApp.Server.DTOs
{
    public class SearchFilterDto
    {
        public int UserId { get; set; }          // <-- добавить
        public string? Gender { get; set; }
        public int? AgeFrom { get; set; }
        public int? AgeTo { get; set; }
        public string? City { get; set; }
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 20;
    }
}