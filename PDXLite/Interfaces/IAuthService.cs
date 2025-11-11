using PDXLite.DTOs.Auth;
using PDXLite.Models;

namespace PDXLite.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse?> RegisterAsync(RegisterRequest request);
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> ConfirmEmailAsync(string token);
        Task<bool> ResendConfirmationEmailAsync(string email);
    }
}
