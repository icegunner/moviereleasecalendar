using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Raven.Client.Documents;
using MovieReleaseCalendar.API.Models;
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
        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        // Ensure database exists
        var dbName = store.Database;
        var dbRecord = await store.Maintenance.Server.SendAsync(new Raven.Client.ServerWide.Operations.GetDatabaseRecordOperation(dbName));
        if (dbRecord == null)
        {
            _logger.LogInformation($"StartupSeeder: Database '{dbName}' does not exist. Creating...");
            await store.Maintenance.Server.SendAsync(new Raven.Client.ServerWide.Operations.CreateDatabaseOperation(new Raven.Client.ServerWide.DatabaseRecord(dbName)));
            _logger.LogInformation($"StartupSeeder: Database '{dbName}' created.");
        }

        using var session = store.OpenAsyncSession();
        var hasMovies = await session.Query<Movie>().AnyAsync();
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
