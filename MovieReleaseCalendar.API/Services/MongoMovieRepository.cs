using MovieReleaseCalendar.API.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
    public class MongoMovieRepository : IMovieRepository
    {
        private readonly IMongoCollection<Movie> _collection;
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly ILogger<MongoMovieRepository> _logger;

        public MongoMovieRepository(string connectionString, string dbName, ILogger<MongoMovieRepository> logger)
        {
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(dbName);
            _collection = _database.GetCollection<Movie>("movies");
            _logger = logger;
        }

        public Task EnsureDatabaseReadyAsync()
        {
            // MongoDB creates databases and collections on first write automatically
            _logger.LogInformation("MongoDB databases are created on first write; no explicit creation needed.");
            return Task.CompletedTask;
        }

        public async Task<bool> HasMoviesAsync()
        {
            return await _collection.Find(Builders<Movie>.Filter.Empty).Limit(1).AnyAsync();
        }

        public async Task<List<Movie>> GetAllMoviesAsync()
        {
            return await _collection.Find(Builders<Movie>.Filter.Empty).ToListAsync();
        }

        public async Task<Movie> GetMovieByIdAsync(string id)
        {
            return await _collection.Find(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<Movie>> GetMoviesByYearAsync(int year)
		{
			var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var end = new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var filter = Builders<Movie>.Filter.Gte(m => m.ReleaseDate, start)
				& Builders<Movie>.Filter.Lt(m => m.ReleaseDate, end);
			return await _collection.Find(filter).ToListAsync();
		}

        public async Task<List<Movie>> GetMoviesByYearsAsync(int[] years)
        {
            var filters = years.Select(y =>
            {
                var start = new DateTime(y, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var end = new DateTime(y + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return Builders<Movie>.Filter.Gte(m => m.ReleaseDate, start)
                     & Builders<Movie>.Filter.Lt(m => m.ReleaseDate, end);
            });
            var filter = Builders<Movie>.Filter.Or(filters);
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<List<Movie>> GetMoviesInRangeAsync(DateTime start, DateTime end)
        {
            var filter = Builders<Movie>.Filter.Gte(m => m.ReleaseDate, start)
                & Builders<Movie>.Filter.Lte(m => m.ReleaseDate, end);
            return await _collection.Find(filter).SortBy(m => m.ReleaseDate).ToListAsync();
        }

        public async Task AddMovieAsync(Movie movie)
        {
            var filter = Builders<Movie>.Filter.Eq(m => m.Id, movie.Id);
            await _collection.ReplaceOneAsync(filter, movie, new ReplaceOptions { IsUpsert = true });
        }

        public async Task UpdateMovieAsync(Movie movie)
        {
            await _collection.ReplaceOneAsync(m => m.Id == movie.Id, movie);
        }

        public async Task DeleteMovieAsync(string id)
        {
            var filter = Builders<Movie>.Filter.Eq(m => m.Id, id);
            await _collection.DeleteOneAsync(filter);
        }

        public async Task DeleteMoviesAsync(IEnumerable<string> ids)
        {
            var idList = ids.ToList();
            if (idList.Count == 0) return;
            var filter = Builders<Movie>.Filter.In(m => m.Id, idList);
            await _collection.DeleteManyAsync(filter);
        }

        public Task SaveChangesAsync() => Task.CompletedTask; // MongoDB is immediate
    }
}
