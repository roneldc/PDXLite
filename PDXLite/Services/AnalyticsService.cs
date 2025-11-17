using Microsoft.EntityFrameworkCore;
using PDXLite.Data;
using PDXLite.Interfaces;
using PDXLite.Models;

namespace PDXLite.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(AppDbContext context, ILogger<AnalyticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task TrackPageVisitAsync(string page, string? ipAddress, string? userAgent, string? referrer)
        {
            try
            {
                var visit = new PageVisit
                {
                    Page = page,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Referrer = referrer,
                    VisitedAt = DateTime.UtcNow
                };

                _context.PageVisits.Add(visit);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Page visit tracked: {Page} from IP: {IP}", page, ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track page visit for page: {Page}", page);
                // Don't throw - analytics shouldn't break the app
            }
        }

        public async Task<Dictionary<string, object>> GetAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var visits = await _context.PageVisits
                .Where(v => v.VisitedAt >= start && v.VisitedAt <= end)
                .ToListAsync();

            var totalVisits = visits.Count;
            var uniqueIps = visits.Select(v => v.IpAddress).Distinct().Count();

            var pageViews = visits
                .GroupBy(v => v.Page)
                .Select(g => new { Page = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            var dailyVisits = visits
                .GroupBy(v => v.VisitedAt.Date)
                .Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"), Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();

            var topReferrers = visits
                .Where(v => !string.IsNullOrEmpty(v.Referrer))
                .GroupBy(v => v.Referrer)
                .Select(g => new { Referrer = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            return new Dictionary<string, object>
        {
            { "totalVisits", totalVisits },
            { "uniqueVisitors", uniqueIps },
            { "pageViews", pageViews },
            { "dailyVisits", dailyVisits },
            { "topReferrers", topReferrers },
            { "startDate", start.ToString("yyyy-MM-dd") },
            { "endDate", end.ToString("yyyy-MM-dd") }
        };
        }
    }
}
