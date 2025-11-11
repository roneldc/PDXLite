using System.ComponentModel.DataAnnotations;

namespace PDXLite.Models
{
    public class ExtractionHistory
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        public User? User { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public string ExtractedText { get; set; } = string.Empty;

        public string StructuredJson { get; set; } = string.Empty;

        public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;

        public int PageCount { get; set; }

        public string? IpAddress { get; set; }

        public bool IsAnonymous { get; set; } = true;
    }
}
