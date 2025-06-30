using System.Threading.Tasks;
using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Background;
using MovieReleaseCalendar.API.Data;
using MovieReleaseCalendar.API.Models;
using MovieReleaseCalendar.API.Services;
using NLog;
using NLog.Web;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Json.Serialization.NewtonsoftJson;
using Newtonsoft.Json.Serialization;

public class Program
{
    public static void Main(string[] args)
    {
        string FirstCharToLower(string str) => $"{char.ToLower(str[0])}{str.Substring(1)}";

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
                .AddSingleton<IDocumentStore>(provider =>
                {
                    var config = builder.Configuration.GetSection("RavenDb");
                    var store = new DocumentStore
                    {
                        Urls = [config["Url"] ?? "http://localhost:8080"],
                        Database = config["Database"] ?? "MovieReleaseCalendar"
                    };
                    store.Conventions.Serialization = new NewtonsoftJsonSerializationConventions
                    {
                        CustomizeJsonSerializer = s => s.ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };
                    store.Conventions.PropertyNameConverter = memberInfo => FirstCharToLower(memberInfo.Name);
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
                .AddScoped<ICalendarService, CalendarService>()
                .AddScoped<IScraperService, ScraperService>()
                .AddHttpClient()
                .AddHostedService<ScrapingWorker>()
                .AddHostedService<StartupSeeder>()

                // Add controllers and static files
                .AddEndpointsApiExplorer()
                .AddRouting()
                .AddControllers();

            var app = builder.Build();

            // Log global startup info using DI logger
            var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
            var assembly = typeof(Program).Assembly;
            var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
            var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
            var description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
            startupLogger.LogInformation($"{product} ({copyright}) - {description}");
            startupLogger.LogInformation($"Application starting. Version: {typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"}.");

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
