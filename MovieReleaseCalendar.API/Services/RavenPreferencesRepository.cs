using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Models;
using Raven.Client.Documents;
using System;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
    public class RavenPreferencesRepository : IPreferencesRepository
    {
        private readonly IDocumentStore _store;
        private readonly ILogger<RavenPreferencesRepository> _logger;

        public RavenPreferencesRepository(IDocumentStore store, ILogger<RavenPreferencesRepository> logger)
        {
            _store = store;
            _logger = logger;
        }

        public async Task<UserPreferences> GetPreferencesAsync()
        {
            using var session = _store.OpenAsyncSession();
            var prefs = await session.LoadAsync<UserPreferences>("userpreferences/global");
            if (prefs == null)
            {
                _logger.LogInformation("No preferences found in RavenDB. Creating defaults.");
                prefs = new UserPreferences();
                await session.StoreAsync(prefs, "userpreferences/global");
                await session.SaveChangesAsync();
            }
            return prefs;
        }

        public async Task SavePreferencesAsync(UserPreferences prefs)
        {
            using var session = _store.OpenAsyncSession();
            prefs.Id = "global";
            prefs.UpdatedAt = DateTimeOffset.UtcNow;
            await session.StoreAsync(prefs, "userpreferences/global");
            await session.SaveChangesAsync();
            _logger.LogInformation("Preferences saved to RavenDB.");
        }
    }
}
