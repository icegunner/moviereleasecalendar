using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MovieReleaseCalendar.API.Models;
using System;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
    public class MongoPreferencesRepository : IPreferencesRepository
    {
        private readonly IMongoCollection<UserPreferences> _collection;
        private readonly ILogger<MongoPreferencesRepository> _logger;

        public MongoPreferencesRepository(IMongoDatabase database, ILogger<MongoPreferencesRepository> logger)
        {
            _collection = database.GetCollection<UserPreferences>("userpreferences");
            _logger = logger;
        }

        public async Task<UserPreferences> GetPreferencesAsync()
        {
            var prefs = await _collection.Find(p => p.Id == "global").FirstOrDefaultAsync();
            if (prefs == null)
            {
                _logger.LogInformation("No preferences found in MongoDB. Creating defaults.");
                prefs = new UserPreferences();
                await _collection.InsertOneAsync(prefs);
            }
            return prefs;
        }

        public async Task SavePreferencesAsync(UserPreferences prefs)
        {
            prefs.Id = "global";
            prefs.UpdatedAt = DateTimeOffset.UtcNow;
            var filter = Builders<UserPreferences>.Filter.Eq(p => p.Id, "global");
            await _collection.ReplaceOneAsync(filter, prefs, new ReplaceOptions { IsUpsert = true });
            _logger.LogInformation("Preferences saved to MongoDB.");
        }
    }
}
