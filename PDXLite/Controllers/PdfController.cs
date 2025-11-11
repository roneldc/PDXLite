using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDXLite.Data;
using PDXLite.DTOs.Error;
using PDXLite.DTOs.Pdf;
using PDXLite.DTOs.RateLimit;
using PDXLite.Interfaces;
using PDXLite.Models;
using System.Security.Claims;
using System.Text.Json;

namespace PDXLite.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PdfController : ControllerBase
    {
        private readonly IPdfService _pdfService;
        private readonly IGeminiService _geminiService;
        private readonly AppDbContext _context;
        private readonly IRateLimitService _rateLimitService;
        private readonly ILogger<PdfController> _logger;
        private readonly IConfiguration _configuration;

        public PdfController(
            IPdfService pdfService,
            IGeminiService geminiService,
            AppDbContext context,
            IRateLimitService rateLimitService,
            ILogger<PdfController> logger,
            IConfiguration configuration)
        {
            _pdfService = pdfService;
            _geminiService = geminiService;
            _context = context;
            _rateLimitService = rateLimitService;
            _logger = logger;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("extract")]
        public async Task<IActionResult> ExtractFromPdf(IFormFile file, [FromHeader(Name = "X-API-Key")] string? apiKey = null)
        {
            var requestId = HttpContext.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();

            _logger.LogInformation("PDF extraction request started. RequestId: {RequestId}, FileName: {FileName}",
                requestId, file?.FileName ?? "none");

            // Validate file
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("PDF extraction failed - no file provided. RequestId: {RequestId}", requestId);

                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.FILE_REQUIRED,
                        Message = "No file uploaded. Please select a PDF file to extract."
                    },
                    Path = Request.Path
                });
            }

            // Validate file extension
            var allowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>() ?? new[] { ".pdf" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                _logger.LogWarning("Invalid file type uploaded: {FileType}. RequestId: {RequestId}",
                    fileExtension, requestId);

                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.INVALID_FILE_TYPE,
                        Message = $"Only PDF files are supported. Received: {fileExtension}"
                    },
                    Path = Request.Path
                });
            }

            // Validate file size
            var maxFileSize = _configuration.GetValue<long>("FileUpload:MaxFileSizeBytes", 10485760); // 10MB default
            if (file.Length > maxFileSize)
            {
                _logger.LogWarning("File too large: {FileSize} bytes (max: {MaxSize}). RequestId: {RequestId}",
                    file.Length, maxFileSize, requestId);

                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.FILE_TOO_LARGE,
                        Message = $"File size exceeds maximum allowed size of {maxFileSize / 1024 / 1024}MB"
                    },
                    Path = Request.Path
                });
            }

            try
            {
                // Check if user is authenticated via API key or JWT
                int? userId = null;
                bool isAuthenticated = false;
                RateLimitType rateLimitType = RateLimitType.Anonymous;
                string rateLimitIdentifier = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Check API key first
                if (!string.IsNullOrEmpty(apiKey))
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.ApiKey == apiKey && u.IsActive);
                    if (user != null)
                    {
                        userId = user.Id;
                        isAuthenticated = true;
                        rateLimitType = RateLimitType.ApiKey;
                        rateLimitIdentifier = apiKey;

                        _logger.LogInformation("Request authenticated via API key. UserId: {UserId}, RequestId: {RequestId}",
                            userId, requestId);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid API key provided. RequestId: {RequestId}", requestId);

                        return Unauthorized(new ErrorResponse
                        {
                            Error = new ErrorDetail
                            {
                                Code = ErrorCodes.INVALID_API_KEY,
                                Message = "Invalid API key provided"
                            },
                            Path = Request.Path
                        });
                    }
                }
                // Then check JWT token
                else if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedUserId))
                    {
                        userId = parsedUserId;
                        isAuthenticated = true;
                        rateLimitType = RateLimitType.Authenticated;
                        rateLimitIdentifier = userId.ToString();

                        _logger.LogInformation("Request authenticated via JWT. UserId: {UserId}, RequestId: {RequestId}",
                            userId, requestId);
                    }
                }
                else
                {
                    _logger.LogInformation("Anonymous request. IP: {IP}, RequestId: {RequestId}",
                        rateLimitIdentifier, requestId);
                }

                // Rate limiting
                var rateLimitInfo = await _rateLimitService.GetRateLimitInfoAsync(rateLimitIdentifier, rateLimitType);

                Response.Headers.Add("X-RateLimit-Limit", rateLimitInfo.Limit.ToString());
                Response.Headers.Add("X-RateLimit-Remaining", rateLimitInfo.Remaining.ToString());
                Response.Headers.Add("X-RateLimit-Reset", rateLimitInfo.ResetTime.ToString("o"));

                if (!rateLimitInfo.IsAllowed)
                {
                    _logger.LogWarning("Rate limit exceeded for {LimitType}. Identifier: {Identifier}, RequestId: {RequestId}",
                        rateLimitType, rateLimitIdentifier, requestId);

                    return StatusCode(429, new ErrorResponse
                    {
                        Error = new ErrorDetail
                        {
                            Code = ErrorCodes.RATE_LIMIT_EXCEEDED,
                            Message = $"Rate limit exceeded. Please try again after {rateLimitInfo.ResetTime:HH:mm:ss}"
                        },
                        Path = Request.Path
                    });
                }

                _logger.LogInformation("Extracting text from PDF. FileName: {FileName}, Size: {Size} bytes, RequestId: {RequestId}",
                    file.FileName, file.Length, requestId);

                // Extract text from PDF
                using var stream = file.OpenReadStream();
                var (extractedText, pageCount) = await _pdfService.ExtractTextFromPdfAsync(stream);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    _logger.LogWarning("No text extracted from PDF. FileName: {FileName}, RequestId: {RequestId}",
                        file.FileName, requestId);

                    return BadRequest(new ErrorResponse
                    {
                        Error = new ErrorDetail
                        {
                            Code = ErrorCodes.EMPTY_PDF,
                            Message = "No text could be extracted from the PDF. The file might be empty, image-only, or corrupted."
                        },
                        Path = Request.Path
                    });
                }

                _logger.LogInformation("Text extracted successfully. Pages: {PageCount}, Characters: {CharCount}, RequestId: {RequestId}",
                    pageCount, extractedText.Length, requestId);

                _logger.LogInformation("Sending text to Gemini for analysis. RequestId: {RequestId}", requestId);

                // Use Gemini to structure the data
                var structuredJson = await _geminiService.AnalyzeTextAndGenerateJsonAsync(extractedText);

                _logger.LogInformation("Gemini analysis completed. RequestId: {RequestId}", requestId);

                // Save to database (only if authenticated)
                if (isAuthenticated && userId.HasValue)
                {
                    var history = new ExtractionHistory
                    {
                        UserId = userId.Value,
                        FileName = file.FileName,
                        FileSize = file.Length,
                        ExtractedText = extractedText,
                        StructuredJson = structuredJson,
                        PageCount = pageCount,
                        ExtractedAt = DateTime.UtcNow,
                        IsAnonymous = false,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    };

                    _context.ExtractionHistories.Add(history);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Extraction saved to history. HistoryId: {HistoryId}, UserId: {UserId}, RequestId: {RequestId}",
                        history.Id, userId, requestId);
                }

                // Parse JSON for response
                var jsonObject = JsonSerializer.Deserialize<object>(structuredJson);

                _logger.LogInformation("PDF extraction completed successfully. RequestId: {RequestId}", requestId);

                return Ok(new PdfExtractionResponse
                {
                    Success = true,
                    Message = isAuthenticated
                        ? "PDF processed successfully and saved to history"
                        : "PDF processed successfully (sign up to save history)",
                    Data = jsonObject,
                    Metadata = new PdfMetadata
                    {
                        FileName = file.FileName,
                        FileSize = file.Length,
                        PageCount = pageCount,
                        ExtractedAt = DateTime.UtcNow
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Configuration error during PDF extraction. RequestId: {RequestId}", requestId);

                return StatusCode(500, new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.CONFIGURATION_ERROR,
                        Message = "Service configuration error. Please contact support."
                    },
                    Path = Request.Path
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Gemini API error during PDF extraction. RequestId: {RequestId}", requestId);

                return StatusCode(503, new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.AI_PROCESSING_FAILED,
                        Message = "AI service is temporarily unavailable. Please try again later."
                    },
                    Path = Request.Path
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during PDF extraction. FileName: {FileName}, RequestId: {RequestId}",
                    file.FileName, requestId);

                return StatusCode(500, new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.PDF_EXTRACTION_FAILED,
                        Message = "An error occurred while processing the PDF. Please try again."
                    },
                    Path = Request.Path
                });
            }
        }

        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning("Unauthorized history access attempt");

                return Unauthorized(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.INVALID_TOKEN,
                        Message = "Invalid or expired authentication token"
                    },
                    Path = Request.Path
                });
            }

            _logger.LogInformation("Fetching extraction history. UserId: {UserId}, Page: {Page}, PageSize: {PageSize}",
                userId, page, pageSize);

            var query = _context.ExtractionHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.ExtractedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new
                {
                    h.Id,
                    h.FileName,
                    h.FileSize,
                    h.PageCount,
                    h.ExtractedAt,
                    HasData = !string.IsNullOrEmpty(h.StructuredJson)
                })
                .ToListAsync();

            _logger.LogInformation("History retrieved successfully. UserId: {UserId}, Total: {Total}, Returned: {Count}",
                userId, total, items.Count);

            return Ok(new
            {
                total,
                page,
                pageSize,
                items
            });
        }

        [HttpGet("history/{id}")]
        [Authorize]
        public async Task<IActionResult> GetHistoryDetail(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning("Unauthorized history detail access attempt");

                return Unauthorized(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.INVALID_TOKEN,
                        Message = "Invalid or expired authentication token"
                    },
                    Path = Request.Path
                });
            }

            _logger.LogInformation("Fetching history detail. HistoryId: {HistoryId}, UserId: {UserId}", id, userId);

            var history = await _context.ExtractionHistories
                .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);

            if (history == null)
            {
                _logger.LogWarning("History not found or access denied. HistoryId: {HistoryId}, UserId: {UserId}",
                    id, userId);

                return NotFound(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = ErrorCodes.RESOURCE_NOT_FOUND,
                        Message = "Extraction history not found"
                    },
                    Path = Request.Path
                });
            }

            var jsonObject = JsonSerializer.Deserialize<object>(history.StructuredJson);

            _logger.LogInformation("History detail retrieved successfully. HistoryId: {HistoryId}, UserId: {UserId}",
                id, userId);

            return Ok(new
            {
                history.Id,
                history.FileName,
                history.FileSize,
                history.PageCount,
                history.ExtractedAt,
                data = jsonObject,
                extractedText = history.ExtractedText
            });
        }
    }
}
