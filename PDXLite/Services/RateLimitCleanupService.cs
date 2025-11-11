using PDXLite.Interfaces;

namespace PDXLite.Services
{
    public class RateLimitCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RateLimitCleanupService> _logger;

        public RateLimitCleanupService(IServiceProvider serviceProvider, ILogger<RateLimitCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Rate limit cleanup service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var rateLimitService = scope.ServiceProvider.GetRequiredService<IRateLimitService>();

                    if (rateLimitService is RateLimitService service)
                    {
                        service.CleanupOldEntries();
                    }

                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in rate limit cleanup service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Rate limit cleanup service stopped");
        }
    }
}
