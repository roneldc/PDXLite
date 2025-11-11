using System.ComponentModel.DataAnnotations;

namespace PDXLite.DTOs.Auth
{
    public class EmailConfirmRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
