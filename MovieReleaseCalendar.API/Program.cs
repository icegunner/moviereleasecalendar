using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Background;
using MovieReleaseCalendar.API.Data;
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

            // Add Swagger/OpenAPI services (conditionally enabled via preferences)
            builder.Services.AddSwaggerGen();

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
                .AddControllers()
                .AddNewtonsoftJson();

            // Configure CORS for React dev server
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

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
                    builder.Services.AddSingleton<IMovieRepository>(sp => new MongoMovieRepository(mongoConn, mongoDb, sp.GetRequiredService<ILogger<MongoMovieRepository>>()));
                    builder.Services.AddSingleton<IPreferencesRepository>(sp =>
                    {
                        var mongoClient = new MongoDB.Driver.MongoClient(mongoConn);
                        var database = mongoClient.GetDatabase(mongoDb);
                        return new MongoPreferencesRepository(database, sp.GetRequiredService<ILogger<MongoPreferencesRepository>>());
                    });
                    dbToLog = "MongoDB";
                    break;
                case "postgres":
                case "postgresql":
                    var pgConn = builder.Configuration["PostgreSql:ConnectionString"] ?? Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTIONSTRING") ?? "Host=localhost;Database=MovieReleaseCalendar;Username=postgres;Password=postgres";
                    builder.Services.AddDbContextFactory<MovieDbContext>(options => options.UseNpgsql(pgConn));
                    builder.Services.AddScoped<IMovieRepository, EfMovieRepository>();
                    builder.Services.AddScoped<IPreferencesRepository, EfPreferencesRepository>();
                    dbToLog = "PostgreSQL";
                    break;
                case "sqlite":
                    var sqlitePath = builder.Configuration["Sqlite:Path"] ?? Environment.GetEnvironmentVariable("SQLITE_PATH") ?? "data/movies.db";
                    var sqliteConn = $"Data Source={sqlitePath}";
                    builder.Services.AddDbContextFactory<MovieDbContext>(options => options.UseSqlite(sqliteConn));
                    builder.Services.AddScoped<IMovieRepository, EfMovieRepository>();
                    builder.Services.AddScoped<IPreferencesRepository, EfPreferencesRepository>();
                    dbToLog = $"SQLite ({sqlitePath})";
                    break;
                case "maria":
                case "mariadb":
                case "mysql":
                    var mariaConn = builder.Configuration["MariaDb:ConnectionString"] ?? Environment.GetEnvironmentVariable("MARIADB_CONNECTIONSTRING") ?? "Server=localhost;Database=MovieReleaseCalendar;User=root;Password=root";
                    var serverVersion = ServerVersion.AutoDetect(mariaConn);
                    builder.Services.AddDbContextFactory<MovieDbContext>(options => options.UseMySql(mariaConn, serverVersion));
                    builder.Services.AddScoped<IMovieRepository, EfMovieRepository>();
                    builder.Services.AddScoped<IPreferencesRepository, EfPreferencesRepository>();
                    dbToLog = "MariaDB";
                    break;
                default:
                    // Register RavenDB Document Store only if using RavenDB
                    var ravenUrl = builder.Configuration["RavenDb:Url"] ?? Environment.GetEnvironmentVariable("RAVENDB_URL") ?? "http://localhost:8080";
                    var ravenDb = builder.Configuration["RavenDb:Database"] ?? Environment.GetEnvironmentVariable("RAVENDB_DATABASE") ?? "MovieReleaseCalendar";
                    builder.Services.AddSingleton(provider => RavenMovieRepository.CreateDocumentStore(ravenUrl, ravenDb));
                    builder.Services.AddScoped<IMovieRepository, RavenMovieRepository>();
                    builder.Services.AddScoped<IPreferencesRepository>(sp =>
                        new RavenPreferencesRepository(
                            sp.GetRequiredService<Raven.Client.Documents.IDocumentStore>(),
                            sp.GetRequiredService<ILogger<RavenPreferencesRepository>>()));
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

            // Gate Swagger access: check DB preference per request.
            // This middleware MUST come before UseSwagger/UseSwaggerUI so it
            // can short-circuit before Swagger handles the request.
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/swagger"))
                {
                    var enableSwagger = builder.Configuration.GetValue<bool>("Swagger:Enabled", false);
                    try
                    {
                        using var scope = app.Services.CreateScope();
                        var prefsRepo = scope.ServiceProvider.GetRequiredService<IPreferencesRepository>();
                        var dbPrefs = await prefsRepo.GetPreferencesAsync();
                        if (dbPrefs != null)
                            enableSwagger = dbPrefs.EnableSwagger;
                    }
                    catch { /* fall back to config */ }

                    if (!enableSwagger)
                    {
                        context.Response.StatusCode = 404;
                        return;
                    }
                }
                await next();
            });

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors();
            app.MapControllers();
            app.MapFallbackToFile("index.html");

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
