using PDXLite.DTOs.RateLimit;
using PDXLite.Interfaces;
using System.Collections.Concurrent;

namespace PDXLite.Services
{
    public class RateLimitService : IRateLimitService
    {
        private readonly ConcurrentDictionary<string, List<DateTime>> _requestLog = new();
        private readonly IConfiguration _configuration;
        private readonly ILogger<RateLimitService> _logger;

        public RateLimitService(IConfiguration configuration, ILogger<RateLimitService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> IsAllowedAsync(string identifier, RateLimitType limitType)
        {
            var info = await GetRateLimitInfoAsync(identifier, limitType);
            return info.IsAllowed;
        }

        public async Task<RateLimitInfo> GetRateLimitInfoAsync(string identifier, RateLimitType limitType)
        {
            return await Task.Run(() =>
            {
                var (limit, windowMinutes) = GetLimitConfig(limitType);
                var key = $"{limitType}:{identifier}";
                var now = DateTime.UtcNow;
                var windowStart = now.AddMinutes(-windowMinutes);

                // Get or create request log for this identifier
                var requests = _requestLog.GetOrAdd(key, _ => new List<DateTime>());

                lock (requests)
                {
                    // Remove old requests outside the window
                    requests.RemoveAll(r => r < windowStart);

                    var remaining = limit - requests.Count;
                    var isAllowed = requests.Count < limit;

                    if (isAllowed)
                    {
                        requests.Add(now);
                        _logger.LogDebug(
                            "Rate limit check passed for {Identifier} ({LimitType}). Requests: {Count}/{Limit}",
                            identifier, limitType, requests.Count, limit);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Rate limit exceeded for {Identifier} ({LimitType}). Limit: {Limit} requests per {Window} minutes",
                            identifier, limitType, limit, windowMinutes);
                    }

                    var oldestRequest = requests.Any() ? requests.Min() : now;
                    var resetTime = oldestRequest.AddMinutes(windowMinutes);

                    return new RateLimitInfo
                    {
                        IsAllowed = isAllowed,
                        Remaining = Math.Max(0, remaining),
                        Limit = limit,
                        ResetTime = resetTime
                    };
                }
            });
        }

        private (int limit, int windowMinutes) GetLimitConfig(RateLimitType limitType)
        {
            return limitType switch
            {
                RateLimitType.Anonymous => (
                    _configuration.GetValue<int>("RateLimiting:Anonymous:PermitLimit", 5),
                    _configuration.GetValue<int>("RateLimiting:Anonymous:WindowMinutes", 60)
                ),
                RateLimitType.Authenticated => (
                    _configuration.GetValue<int>("RateLimiting:Authenticated:PermitLimit", 100),
                    _configuration.GetValue<int>("RateLimiting:Authenticated:WindowMinutes", 60)
                ),
                RateLimitType.ApiKey => (
                    _configuration.GetValue<int>("RateLimiting:ApiKey:PermitLimit", 1000),
                    _configuration.GetValue<int>("RateLimiting:ApiKey:WindowHours", 24) * 60
                ),
                _ => (10, 60)
            };
        }

        // Cleanup old entries periodically
        public void CleanupOldEntries()
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-25); // Keep last 25 hours
            var keysToRemove = new List<string>();

            foreach (var kvp in _requestLog)
            {
                var requests = kvp.Value;
                lock (requests)
                {
                    requests.RemoveAll(r => r < cutoffTime);
                    if (!requests.Any())
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
            }

            foreach (var key in keysToRemove)
            {
                _requestLog.TryRemove(key, out _);
            }

            _logger.LogInformation("Rate limit cleanup completed. Removed {Count} empty entries", keysToRemove.Count);
        }
    }
}