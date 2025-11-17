namespace PDXLite.Interfaces
{
    public interface IAnalyticsService
    {
        Task TrackPageVisitAsync(string page, string? ipAddress, string? userAgent, string? referrer);
        Task<Dictionary<string, object>> GetAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}
