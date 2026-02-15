using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using MovieReleaseCalendar.API.Services;
using System;

public class StartupSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartupSeeder> _logger;

    public StartupSeeder(IServiceProvider serviceProvider, ILogger<StartupSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StartupSeeder: Checking for existing movies in database...");
        using var scope = _serviceProvider.CreateScope();
        var movieRepository = scope.ServiceProvider.GetRequiredService<IMovieRepository>();

        // Ensure database is ready (e.g., RavenDB database creation)
        await movieRepository.EnsureDatabaseReadyAsync();

        var hasMovies = await movieRepository.HasMoviesAsync();
        if (!hasMovies)
        {
            _logger.LogInformation("StartupSeeder: No movies found. Running scraper...");
            var scraper = scope.ServiceProvider.GetRequiredService<IScraperService>();
            await scraper.ScrapeAsync();
            _logger.LogInformation("StartupSeeder: Scraper completed.");
        }
        else
        {
            _logger.LogInformation("StartupSeeder: Movies already exist. Skipping seeding.");
        }

        _logger.LogInformation("StartupSeeder: Seeding completed.");
        _logger.LogInformation("Ready to accept requests.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
