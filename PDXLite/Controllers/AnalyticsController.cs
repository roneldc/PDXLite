using Microsoft.AspNetCore.Mvc;
using PDXLite.Interfaces;

namespace PDXLite.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(
            IAnalyticsService analyticsService,
            IConfiguration configuration,
            ILogger<AnalyticsController> logger)
        {
            _analyticsService = analyticsService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAnalytics(
    [FromQuery] DateTime? startDate = null,
    [FromQuery] DateTime? endDate = null)
        {
            _logger.LogInformation("Analytics requested. StartDate: {StartDate}, EndDate: {EndDate}",
                startDate, endDate);

            try
            {
                var analytics = await _analyticsService.GetAnalyticsAsync(startDate, endDate);

                // Add app info
                analytics["appName"] = _configuration["AppSettings:AppName"] ?? "PDXLite";
                analytics["appUrl"] = _configuration["AppSettings:AppUrl"] ?? "https://localhost:5001";
                analytics["environment"] = _configuration["AppSettings:Environment"] ?? "Development";

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve analytics");
                return StatusCode(500, new { error = "Failed to retrieve analytics" });
            }
        }

        [HttpGet("app-info")]
        public IActionResult GetAppInfo()
        {
            var appInfo = new
            {
                appName = _configuration["AppSettings:AppName"] ?? "PDXLite",
                appUrl = _configuration["AppSettings:AppUrl"] ?? "https://localhost:5001",
                environment = _configuration["AppSettings:Environment"] ?? "Development",
                version = "1.0.0"
            };

            return Ok(appInfo);
        }
    }
}
