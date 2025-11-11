using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PDXLite.Data;
using PDXLite.DTOs.Auth;
using PDXLite.Interfaces;
using PDXLite.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PDXLite.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AppDbContext context,
            IConfiguration configuration,
            IEmailService emailService,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
        {
            // Check if user exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
                return null;
            }

            // Generate unique API key
            var apiKey = GenerateApiKey();

            // Generate email confirmation token
            var confirmationToken = GenerateEmailConfirmationToken();

            // Create user
            var user = new User
            {
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = HashPassword(request.Password),
                ApiKey = apiKey,
                EmailConfirmed = false,
                EmailConfirmationToken = confirmationToken,
                EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User registered successfully: {Email}, UserId: {UserId}", request.Email, user.Id);

            // Send confirmation email
            try
            {
                await _emailService.SendEmailConfirmationAsync(user.Email, user.FullName, confirmationToken);
                _logger.LogInformation("Confirmation email sent to: {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email to: {Email}", user.Email);
            }

            return new AuthResponse
            {
                Token = GenerateJwtToken(user),
                Email = user.Email,
                FullName = user.FullName,
                ApiKey = null, // Don't return API key until email is confirmed
                EmailConfirmed = false,
                Message = "Registration successful! Please check your email to confirm your account."
            };
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
                return null;
            }

            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login attempt with incorrect password for email: {Email}", request.Email);
                return null;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive account: {Email}", request.Email);
                return null;
            }

            var message = !user.EmailConfirmed
                ? "Login successful, but please confirm your email to access all features."
                : null;

            _logger.LogInformation("User logged in successfully: {Email}, EmailConfirmed: {EmailConfirmed}",
                user.Email, user.EmailConfirmed);

            return new AuthResponse
            {
                Token = GenerateJwtToken(user),
                Email = user.Email,
                FullName = user.FullName,
                ApiKey = user.EmailConfirmed ? user.ApiKey : null, // Only return API key if email confirmed
                EmailConfirmed = user.EmailConfirmed,
                Message = message
            };
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> ConfirmEmailAsync(string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);

            if (user == null)
            {
                _logger.LogWarning("Email confirmation attempted with invalid token");
                return false;
            }

            if (user.EmailConfirmed)
            {
                _logger.LogInformation("Email confirmation attempted for already confirmed user: {Email}", user.Email);
                return true; // Already confirmed
            }

            if (user.EmailConfirmationTokenExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("Email confirmation attempted with expired token for user: {Email}", user.Email);
                return false;
            }

            user.EmailConfirmed = true;
            user.EmailConfirmationToken = null;
            user.EmailConfirmationTokenExpiry = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Email confirmed successfully for user: {Email}", user.Email);

            // Send welcome email with API key
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName, user.ApiKey);
                _logger.LogInformation("Welcome email sent to: {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to: {Email}", user.Email);
            }

            return true;
        }

        public async Task<bool> ResendConfirmationEmailAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                _logger.LogWarning("Resend confirmation attempted for non-existent email: {Email}", email);
                return false;
            }

            if (user.EmailConfirmed)
            {
                _logger.LogInformation("Resend confirmation attempted for already confirmed user: {Email}", email);
                return true; // Already confirmed
            }

            // Generate new token
            user.EmailConfirmationToken = GenerateEmailConfirmationToken();
            user.EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(24);

            await _context.SaveChangesAsync();

            _logger.LogInformation("New confirmation token generated for user: {Email}", email);

            // Send confirmation email
            try
            {
                await _emailService.SendEmailConfirmationAsync(user.Email, user.FullName, user.EmailConfirmationToken);
                _logger.LogInformation("Confirmation email resent to: {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend confirmation email to: {Email}", user.Email);
                return false;
            }
        }

        private string HashPassword(string password)
        {
            // Use BCrypt with work factor of 12 (2^12 = 4096 iterations)
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        private bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "YourSuperSecretKeyForPDXLiteApplication2024!");
            var expiryDays = int.Parse(_configuration["Jwt:ExpiryInDays"] ?? "30");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("EmailConfirmed", user.EmailConfirmed.ToString())
            }),
                Expires = DateTime.UtcNow.AddDays(expiryDays),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateApiKey()
        {
            return $"pdx_{Guid.NewGuid():N}{Guid.NewGuid():N}".Substring(0, 48);
        }

        private string GenerateEmailConfirmationToken()
        {
            // Generate a secure random token
            using var rng = RandomNumberGenerator.Create();
            var tokenBytes = new byte[32];
            rng.GetBytes(tokenBytes);
            return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}
