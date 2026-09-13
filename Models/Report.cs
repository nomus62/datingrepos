using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DatingApp.Server.Models;

[Table("Reports")]
public class Report
{
    [Key]
    public int Id { get; set; }
    public int ReporterId { get; set; }
    public int ReportedId { get; set; }
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public int? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
}