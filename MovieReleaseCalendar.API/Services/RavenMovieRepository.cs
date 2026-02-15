using Microsoft.Extensions.Configuration;
using MovieReleaseCalendar.API.Models;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Json.Serialization.NewtonsoftJson;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
    public class RavenMovieRepository : IMovieRepository
    {
        private readonly IDocumentStore _store;
        public RavenMovieRepository(IDocumentStore store)
        {
            _store = store;
        }

        public static IDocumentStore CreateDocumentStore(string ravenUrl, string ravenDb)
        {
            var store = new DocumentStore
            {
                Urls = new[] { ravenUrl },
                Database = ravenDb
            };
            store.Conventions.Serialization = new NewtonsoftJsonSerializationConventions
            {
                CustomizeJsonSerializer = s => s.ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            store.Conventions.PropertyNameConverter = memberInfo => char.ToLower(memberInfo.Name[0]) + memberInfo.Name.Substring(1);
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
        }

        public async Task<List<Movie>> GetAllMoviesAsync()
        {
            using var session = _store.OpenAsyncSession();
            return await session.Query<Movie>().ToListAsync();
        }

        public async Task<Movie> GetMovieByIdAsync(string id)
        {
            using var session = _store.OpenAsyncSession();
            return await session.LoadAsync<Movie>(id);
        }

		public async Task<List<Movie>> GetMoviesByYearAsync(int year)
		{
			using var session = _store.OpenAsyncSession();
			var records = await session.Query<Movie>().ToListAsync();
			return records.Where(m => m.ReleaseDate.Year == year).ToList();
        }

		public async Task<List<Movie>> GetMoviesByYearsAsync(int[] years)
		{
			using var session = _store.OpenAsyncSession();
			var records = await session.Query<Movie>().ToListAsync();
			return records.Where(m => years.Contains(m.ReleaseDate.Year)).ToList();
        }

        public async Task AddMovieAsync(Movie movie)
		{
			using var session = _store.OpenAsyncSession();
			await session.StoreAsync(movie);
			await session.SaveChangesAsync();
		}

        public async Task UpdateMovieAsync(Movie movie)
        {
            using var session = _store.OpenAsyncSession();
            await session.StoreAsync(movie, movie.Id);
            await session.SaveChangesAsync();
        }

        public async Task DeleteMovieAsync(string id)
        {
            using var session = _store.OpenAsyncSession();
            session.Delete(id);
            await session.SaveChangesAsync();
        }

        public Task SaveChangesAsync() => Task.CompletedTask; // Each op is immediate
    }
}
