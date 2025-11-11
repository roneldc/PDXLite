using System.ComponentModel.DataAnnotations;

namespace PDXLite.DTOs.Auth
{
    public class ResendConfirmationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
