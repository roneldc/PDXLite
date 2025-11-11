using PDXLite.DTOs.RateLimit;

namespace PDXLite.Interfaces
{
    public interface IRateLimitService
    {
        Task<bool> IsAllowedAsync(string identifier, RateLimitType limitType);
        Task<RateLimitInfo> GetRateLimitInfoAsync(string identifier, RateLimitType limitType);
    }
}
