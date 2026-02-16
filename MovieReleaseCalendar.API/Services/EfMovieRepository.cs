using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Data;
using MovieReleaseCalendar.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
    public class EfMovieRepository : IMovieRepository
    {
        private readonly IDbContextFactory<MovieDbContext> _contextFactory;
        private readonly ILogger<EfMovieRepository> _logger;

        public EfMovieRepository(IDbContextFactory<MovieDbContext> contextFactory, ILogger<EfMovieRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task EnsureDatabaseReadyAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            await context.Database.EnsureCreatedAsync();
            _logger.LogInformation("EF Core database is ready (EnsureCreated).");
        }

        public async Task<bool> HasMoviesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Movies.AnyAsync();
        }

        public async Task<List<Movie>> GetAllMoviesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Movies.ToListAsync();
        }

        public async Task<Movie> GetMovieByIdAsync(string id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Movies.FindAsync(id);
        }

        public async Task<List<Movie>> GetMoviesByYearAsync(int year)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return await context.Movies
                .Where(m => m.ReleaseDate >= start && m.ReleaseDate < end)
                .ToListAsync();
        }

        public async Task<List<Movie>> GetMoviesByYearsAsync(int[] years)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Movies
                .Where(m => years.Contains(m.ReleaseDate.Year))
                .ToListAsync();
        }

        public async Task<List<Movie>> GetMoviesInRangeAsync(DateTime start, DateTime end)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Movies
                .Where(m => m.ReleaseDate >= start && m.ReleaseDate <= end)
                .OrderBy(m => m.ReleaseDate)
                .ToListAsync();
        }

        public async Task AddMovieAsync(Movie movie)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.Movies.FindAsync(movie.Id);
            if (existing != null)
            {
                context.Entry(existing).CurrentValues.SetValues(movie);
            }
            else
            {
                context.Movies.Add(movie);
            }
            await context.SaveChangesAsync();
        }

        public async Task UpdateMovieAsync(Movie movie)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.Movies.FindAsync(movie.Id);
            if (existing != null)
            {
                context.Entry(existing).CurrentValues.SetValues(movie);
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteMovieAsync(string id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var movie = await context.Movies.FindAsync(id);
            if (movie != null)
            {
                context.Movies.Remove(movie);
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteMoviesAsync(IEnumerable<string> ids)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var idList = ids.ToList();
            if (idList.Count == 0) return;
            var movies = await context.Movies.Where(m => idList.Contains(m.Id)).ToListAsync();
            context.Movies.RemoveRange(movies);
            await context.SaveChangesAsync();
        }

        public Task SaveChangesAsync() => Task.CompletedTask; // Each operation saves immediately
    }
}
