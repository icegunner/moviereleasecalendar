using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Services;
using NCrontab;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Background
{
    public class ScrapingWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScrapingWorker> _logger;
        private CrontabSchedule _cronSchedule;
        private string _cronExpression;
        private readonly IConfiguration _configuration;
        private DateTime _nextRun;

        public ScrapingWorker(IServiceProvider serviceProvider, ILogger<ScrapingWorker> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            LoadCronFromConfig();
        }

        private void LoadCronFromConfig()
        {
            // Check environment variable first
            var envCron = Environment.GetEnvironmentVariable("SCHEDULE");
            if (!string.IsNullOrWhiteSpace(envCron))
            {
                _cronExpression = envCron;
                _logger.LogInformation($"Using cron from SCHEDULE environment variable: {_cronExpression}");
            }
            else
            {
                _logger.LogInformation("Using cron from configuration file.");
                var scrapingSection = _configuration.GetSection("Scraping");
                _cronExpression = scrapingSection["Cron"] ?? "0 0 * * 0"; // Default: every Sunday at 12am
            }
            _cronSchedule = CrontabSchedule.Parse(_cronExpression);
            _nextRun = _cronSchedule.GetNextOccurrence(DateTime.UtcNow);
            _logger.LogInformation($"Next scrape scheduled for {_nextRun:u} using cron expression: {_cronExpression}");
        }

        /// <summary>
        /// Attempts to load the cron schedule from UserPreferences in the database.
        /// Falls back to env var / config if preferences have no schedule set.
        /// </summary>
        private async Task<bool> TryLoadCronFromPreferencesAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var prefsRepo = scope.ServiceProvider.GetRequiredService<IPreferencesRepository>();
                var prefs = await prefsRepo.GetPreferencesAsync();
                if (!string.IsNullOrWhiteSpace(prefs.CronSchedule) && prefs.CronSchedule != _cronExpression)
                {
                    _cronExpression = prefs.CronSchedule;
                    _cronSchedule = CrontabSchedule.Parse(_cronExpression);
                    _nextRun = _cronSchedule.GetNextOccurrence(DateTime.UtcNow);
                    _logger.LogInformation($"Loaded cron from preferences: {_cronExpression}. Next run: {_nextRun:u}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load cron schedule from preferences. Using current schedule.");
            }
            return false;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var scrapingSection = _configuration.GetSection("Scraping");
            var lastCron = _cronExpression;
            while (!stoppingToken.IsCancellationRequested)
            {
                // Check for config changes from env var, config file, or preferences
                var envCron = Environment.GetEnvironmentVariable("SCHEDULE");
                var currentCron = !string.IsNullOrWhiteSpace(envCron) ? envCron : scrapingSection["Cron"];
                if (currentCron != lastCron)
                {
                    _logger.LogInformation($"Detected cron config change: {currentCron}");
                    LoadCronFromConfig();
                    lastCron = currentCron;
                }

                // Also check preferences for cron schedule changes
                await TryLoadCronFromPreferencesAsync();

                var now = DateTime.UtcNow;
				if (_nextRun <= now)
				{
					_logger.LogInformation($"Scrape scheduled for {_nextRun:u} now running.");

					try
					{
						using var scope = _serviceProvider.CreateScope();
						var scraper = scope.ServiceProvider.GetRequiredService<IScraperService>();
						var result = await scraper.ScrapeAsync(stoppingToken);
						_logger.LogInformation($"Scraped {result.NewMovies.Count} new and {result.UpdatedMovies.Count} updated movies at {DateTime.UtcNow:u}");
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error during scheduled scrape.");
					}

					_nextRun = _cronSchedule.GetNextOccurrence(now);
					_logger.LogInformation($"Next scrape is scheduled for {_nextRun:u}");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Check every minute
            }
        }
    }
}
