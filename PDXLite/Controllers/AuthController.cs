using Microsoft.AspNetCore.Mvc;
using PDXLite.DTOs.Auth;
using PDXLite.DTOs.Error;
using PDXLite.DTOs.RateLimit;
using PDXLite.Interfaces;

namespace PDXLite.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IRateLimitService _rateLimitService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, IRateLimitService rateLimitService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _rateLimitService = rateLimitService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            _logger.LogInformation("Registration attempt for email: {Email} from IP: {IP}",
                request.Email, clientIp);

            // Rate limiting
            var rateLimitInfo = await _rateLimitService.GetRateLimitInfoAsync(clientIp, RateLimitType.Anonymous);
            if (!rateLimitInfo.IsAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for registration from IP: {IP}", clientIp);

                Response.Headers.Add("X-RateLimit-Limit", rateLimitInfo.Limit.ToString());
                Response.Headers.Add("X-RateLimit-Remaining", rateLimitInfo.Remaining.ToString());
                Response.Headers.Add("X-RateLimit-Reset", rateLimitInfo.ResetTime.ToString("o"));

                return StatusCode(429, new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.RATE_LIMIT_EXCEEDED,
                        Message = $"Too many registration attempts. Please try again after {rateLimitInfo.ResetTime:HH:mm:ss}"
                    },
                    Path = Request.Path
                });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid registration request for email: {Email}", request.Email);

                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.VALIDATION_ERROR,
                        Message = "Validation failed",
                        Details = ModelState.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                        )
                    },
                    Path = Request.Path
                });
            }

            var result = await _authService.RegisterAsync(request);

            if (result == null)
            {
                _logger.LogWarning("Registration failed - user already exists: {Email}", request.Email);

                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.USER_ALREADY_EXISTS,
                        Message = "A user with this email already exists"
                    },
                    Path = Request.Path
                });
            }

            _logger.LogInformation("User registered successfully: {Email}", request.Email);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            _logger.LogInformation("Login attempt for email: {Email} from IP: {IP}",
                request.Email, clientIp);

            // Rate limiting
            var rateLimitInfo = await _rateLimitService.GetRateLimitInfoAsync(clientIp, RateLimitType.Anonymous);
            if (!rateLimitInfo.IsAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for login from IP: {IP}", clientIp);

                Response.Headers.Add("X-RateLimit-Limit", rateLimitInfo.Limit.ToString());
                Response.Headers.Add("X-RateLimit-Remaining", rateLimitInfo.Remaining.ToString());
                Response.Headers.Add("X-RateLimit-Reset", rateLimitInfo.ResetTime.ToString("o"));

                return StatusCode(429, new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.RATE_LIMIT_EXCEEDED,
                        Message = $"Too many login attempts. Please try again after {rateLimitInfo.ResetTime:HH:mm:ss}"
                    },
                    Path = Request.Path
                });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid login request for email: {Email}", request.Email);

                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.VALIDATION_ERROR,
                        Message = "Validation failed",
                        Details = ModelState.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                        )
                    },
                    Path = Request.Path
                });
            }

            var result = await _authService.LoginAsync(request);

            if (result == null)
            {
                _logger.LogWarning("Login failed for email: {Email}", request.Email);

                return Unauthorized(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.INVALID_CREDENTIALS,
                        Message = "Invalid email or password"
                    },
                    Path = Request.Path
                });
            }

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);
            return Ok(result);
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] EmailConfirmRequest request)
        {
            _logger.LogInformation("Email confirmation attempt with token");

            if (string.IsNullOrEmpty(request.Token))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.VALIDATION_ERROR,
                        Message = "Confirmation token is required"
                    },
                    Path = Request.Path
                });
            }

            var success = await _authService.ConfirmEmailAsync(request.Token);

            if (!success)
            {
                _logger.LogWarning("Email confirmation failed - invalid or expired token");

                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.INVALID_TOKEN,
                        Message = "Invalid or expired confirmation token. Please request a new confirmation email."
                    },
                    Path = Request.Path
                });
            }

            _logger.LogInformation("Email confirmed successfully");

            return Ok(new
            {
                success = true,
                message = "Email confirmed successfully! You can now access all features and your API key has been sent to your email."
            });
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            _logger.LogInformation("Resend confirmation email request for: {Email}", request.Email);

            // Rate limiting
            var rateLimitInfo = await _rateLimitService.GetRateLimitInfoAsync(clientIp, RateLimitType.Anonymous);
            if (!rateLimitInfo.IsAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for resend confirmation from IP: {IP}", clientIp);

                return StatusCode(429, new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.RATE_LIMIT_EXCEEDED,
                        Message = $"Too many requests. Please try again after {rateLimitInfo.ResetTime:HH:mm:ss}"
                    },
                    Path = Request.Path
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.VALIDATION_ERROR,
                        Message = "Valid email address is required"
                    },
                    Path = Request.Path
                });
            }

            var success = await _authService.ResendConfirmationEmailAsync(request.Email);

            if (!success)
            {
                _logger.LogWarning("Resend confirmation failed for email: {Email}", request.Email);

                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.RESOURCE_NOT_FOUND,
                        Message = "User not found or already confirmed"
                    },
                    Path = Request.Path
                });
            }

            _logger.LogInformation("Confirmation email resent successfully to: {Email}", request.Email);

            return Ok(new
            {
                success = true,
                message = "Confirmation email has been resent. Please check your inbox."
            });
        }
    }
}
