using System.ComponentModel.DataAnnotations;

namespace PDXLite.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; } = false;

        public string? EmailConfirmationToken { get; set; }

        public DateTime? EmailConfirmationTokenExpiry { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public ICollection<ExtractionHistory> ExtractionHistories { get; set; } = new List<ExtractionHistory>();
    }
}
