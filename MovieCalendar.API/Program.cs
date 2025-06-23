using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovieCalendar.API.Data;
using MovieCalendar.API.Services;
using MovieCalendar.API.Background;
using Raven.Client.Documents;
using MovieCalendar.API.Models;
using System.Threading.Tasks;
using Raven.Client.Documents.Conventions;
using NLog.Web;
using NLog;
using System;

internal class Program
{
    private static void Main(string[] args)
    {
        // Setup NLog for Dependency injection and configuration from appsettings.json
        LogManager.Setup().LoadConfigurationFromAppSettings();
        var logger = LogManager.GetCurrentClassLogger();
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add NLog to ASP.NET Core
            builder.Host.UseNLog();

            // Load configuration from appsettings.json
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // Register RavenDB Document Store
            builder.Services
                .AddSingleton<IDocumentStore>(provider =>
                {
                    var config = builder.Configuration.GetSection("RavenDb");
                    var store = new DocumentStore
                    {
                        Urls = [config["Url"] ?? "http://localhost:8080"],
                        Database = config["Database"] ?? "MovieReleaseCalendar"
                    };
                    store.Conventions.FindCollectionName = type =>
                    {
                        if (type == typeof(Movie))
                            return "movies";
                        return DocumentConventions.DefaultGetCollectionName(type);
                    };
                    store.Conventions.MaxNumberOfRequestsPerSession = int.MaxValue;
                    store.Conventions.RegisterAsyncIdConvention<Movie>((dbname, metadata) => Task.FromResult($"movie/{metadata.Id}"));
                    store.Initialize();
                    return store;
                })

                // Register services
                .AddScoped<RavenDbDocumentStore>()
                .AddScoped<ScraperService>()
                .AddScoped<CalendarService>()
                .AddHttpClient()
                .AddHostedService<ScrapingWorker>()

                // Add controllers and static files
                .AddEndpointsApiExplorer()
                .AddRouting()
                .AddControllers();

            var app = builder.Build();

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
