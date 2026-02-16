using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Data;
using MovieReleaseCalendar.API.Models;
using System;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
    public class EfPreferencesRepository : IPreferencesRepository
    {
        private readonly IDbContextFactory<MovieDbContext> _contextFactory;
        private readonly ILogger<EfPreferencesRepository> _logger;

        public EfPreferencesRepository(IDbContextFactory<MovieDbContext> contextFactory, ILogger<EfPreferencesRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<UserPreferences> GetPreferencesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var prefs = await context.UserPreferences.FindAsync("global");
            if (prefs == null)
            {
                _logger.LogInformation("No preferences found. Creating defaults.");
                prefs = new UserPreferences();
                context.UserPreferences.Add(prefs);
                await context.SaveChangesAsync();
            }
            return prefs;
        }

        public async Task SavePreferencesAsync(UserPreferences prefs)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            prefs.Id = "global";
            prefs.UpdatedAt = DateTimeOffset.UtcNow;
            var existing = await context.UserPreferences.FindAsync("global");
            if (existing != null)
            {
                context.Entry(existing).CurrentValues.SetValues(prefs);
            }
            else
            {
                context.UserPreferences.Add(prefs);
            }
            await context.SaveChangesAsync();
            _logger.LogInformation("Preferences saved successfully.");
        }
    }
}
