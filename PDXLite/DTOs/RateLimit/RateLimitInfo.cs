namespace PDXLite.DTOs.RateLimit
{
    public class RateLimitInfo
    {
        public bool IsAllowed { get; set; }
        public int Remaining { get; set; }
        public int Limit { get; set; }
        public DateTime ResetTime { get; set; }
    }
}
