using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovieCalendar.API.Data;
using MovieCalendar.API.Services;
using MovieCalendar.API.Background;
using Raven.Client.Documents;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Load configuration from appsettings.json
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        // Register RavenDB Document Store
        builder.Services.AddSingleton<IDocumentStore>(provider =>
        {
            var config = builder.Configuration.GetSection("RavenDb");
            var store = new DocumentStore
            {
                Urls = new[] { config["Url"] ?? "http://localhost:8080" },
                Database = config["Database"] ?? "MovieCalendar"
            };
            store.Initialize();
            return store;
        });

        // Register services
        builder.Services.AddScoped<RavenDbDocumentStore>();
        builder.Services.AddScoped<ScraperService>();
        builder.Services.AddScoped<CalendarService>();
        builder.Services.AddHttpClient();
        builder.Services.AddHostedService<ScrapingWorker>();

        // Add controllers and static files
        builder.Services.AddControllers();
        builder.Services.AddRouting();
        builder.Services.AddEndpointsApiExplorer();

        var app = builder.Build();

        app.UseStaticFiles();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapFallbackToFile("index.html");
        });

        app.Run();
    }
}
