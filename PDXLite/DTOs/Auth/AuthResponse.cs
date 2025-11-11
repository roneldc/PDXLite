namespace PDXLite.DTOs.Auth
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? ApiKey { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? Message { get; set; }
    }
}
