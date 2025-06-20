using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MovieCalendar.API.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MovieCalendar.API.Background
{
    public class ScrapingWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScrapingWorker> _logger;
        private readonly TimeSpan _scrapeTime = TimeSpan.FromHours(24); // Default to daily
        private readonly TimeSpan _runAt = new(2, 0, 0); // 2:00 AM UTC

        public ScrapingWorker(IServiceProvider serviceProvider, ILogger<ScrapingWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var nextRun = now.Date.Add(_runAt);
                if (nextRun < now)
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;
                _logger.LogInformation($"Next scrape scheduled for {nextRun:u} (in {delay.TotalMinutes:F0} minutes)");

                await Task.Delay(delay, stoppingToken);

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var scraper = scope.ServiceProvider.GetRequiredService<ScraperService>();
                    var movies = await scraper.ScrapeAsync();
                    _logger.LogInformation($"Scraped {movies.Count} new movies at {DateTime.UtcNow:u}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during scheduled scrape.");
                }

                await Task.Delay(_scrapeTime, stoppingToken);
            }
        }
    }
}
