using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Background;
using MovieReleaseCalendar.API.Services;
using NLog;
using NLog.Web;

public class Program
{
    public static void Main(string[] args)
    {
        // Setup NLog for Dependency injection and configuration from appsettings.json
        // LogManager.Setup().LoadConfigurationFromAppSettings();
        var logger = LogManager.GetCurrentClassLogger();
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#if DEBUG
                .AddJsonFile("appsettings.Debug.json", optional: true, reloadOnChange: true);
#else
                .AddJsonFile("appsettings.Release.json", optional: true, reloadOnChange: true);
#endif

            builder.Logging
                .ClearProviders()
                .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace)
                .AddFilter("System", Microsoft.Extensions.Logging.LogLevel.Warning)
                .AddFilter("Microsoft", Microsoft.Extensions.Logging.LogLevel.Warning)
                .AddFilter("System.Net.HttpClient", Microsoft.Extensions.Logging.LogLevel.Warning);
            //.AddNLogWeb();

            // Add NLog to ASP.NET Core
            builder.Host.UseNLog();

            // Register RavenDB Document Store
            builder.Services
                // Register repository based on environment variable
                .AddScoped<ICalendarService, CalendarService>()
                .AddScoped<IScraperService, ScraperService>()
                .AddHttpClient()
                .AddHostedService<ScrapingWorker>()
                .AddHostedService<StartupSeeder>()

                // Add controllers and static files
                .AddEndpointsApiExplorer()
                .AddRouting()
                .AddControllers();

            // Register repository based on environment variable
            var dbProvider = Environment.GetEnvironmentVariable("MOVIECALENDAR_DB_PROVIDER")?.ToLowerInvariant() ?? "ravendb";
            var dbToLog = "RavenDB";
            switch (dbProvider)
            {
                case "orient":
                case "orientdb":
                    // OrientDB repository will be registered later
                    dbToLog = "OrientDB";
                    break;
                case "couch":
                case "couchdb":
                    // CouchDB repository will be registered later
                    dbToLog = "CouchDB";
                    break;
                case "mongo":
                case "mongodb":
                    var mongoConn = builder.Configuration["MongoDb:ConnectionString"] ?? Environment.GetEnvironmentVariable("MONGODB_CONNECTIONSTRING") ?? "mongodb://localhost:27017";
                    var mongoDb = builder.Configuration["MongoDb:Database"] ?? Environment.GetEnvironmentVariable("MONGODB_DATABASE") ?? "MovieReleaseCalendar";
                    builder.Services.AddSingleton<IMovieRepository>(sp => new MongoMovieRepository(mongoConn, mongoDb));
                    dbToLog = "MongoDB";
                    break;
                default:
                    // Register RavenDB Document Store only if using RavenDB
                    var ravenUrl = builder.Configuration["RavenDb:Url"] ?? Environment.GetEnvironmentVariable("RAVENDB_URL") ?? "http://localhost:8080";
                    var ravenDb = builder.Configuration["RavenDb:Database"] ?? Environment.GetEnvironmentVariable("RAVENDB_DATABASE") ?? "MovieReleaseCalendar";
                    builder.Services.AddSingleton(provider => RavenMovieRepository.CreateDocumentStore(ravenUrl, ravenDb));
                    builder.Services.AddScoped<IMovieRepository, RavenMovieRepository>();
                    dbToLog = "RavenDB";
                    break;
            }

            var app = builder.Build();

            // Log global startup info using DI logger
            var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
            var assembly = typeof(Program).Assembly;
            var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
            var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
            var description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
            startupLogger.LogInformation($"{product} ({copyright}) - {description}");
            startupLogger.LogInformation($"Application starting. Version: {typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"}.");
            startupLogger.LogInformation($"Using database provider: {dbToLog}");

            app
                .UseStaticFiles()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapFallbackToFile("index.html");
                });

            app.Run();
        }
        catch (Exception ex)
        {
            // NLog: catch setup errors
            logger.Error(ex, "Stopped program because of exception");
            throw;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }
}
