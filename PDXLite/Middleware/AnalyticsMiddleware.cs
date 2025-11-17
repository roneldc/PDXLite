using PDXLite.Interfaces;

namespace PDXLite.Middleware
{
    public class AnalyticsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AnalyticsMiddleware> _logger;

        public AnalyticsMiddleware(RequestDelegate next, ILogger<AnalyticsMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAnalyticsService analyticsService)
        {
            // Only track page visits (GET requests to pages, not API calls or static files)
            var path = context.Request.Path.Value ?? "/";
            var shouldTrack = context.Request.Method == "GET"
                && !path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                && !IsStaticFile(path);

            if (shouldTrack)
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                var userAgent = context.Request.Headers["User-Agent"].ToString();
                var referrer = context.Request.Headers["Referer"].ToString();

                // Track async without blocking the request
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await analyticsService.TrackPageVisitAsync(path, ipAddress, userAgent, referrer);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to track page visit");
                    }
                });
            }

            await _next(context);
        }

        private bool IsStaticFile(string path)
        {
            var staticExtensions = new[] { ".css", ".js", ".jpg", ".jpeg", ".png", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf", ".eot" };
            return staticExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }
    }
}
