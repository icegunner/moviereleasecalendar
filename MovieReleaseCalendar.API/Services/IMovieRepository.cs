using MovieReleaseCalendar.API.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
    public interface IMovieRepository
    {
        Task EnsureDatabaseReadyAsync();
        Task<bool> HasMoviesAsync();
        Task<List<Movie>> GetAllMoviesAsync();
        Task<Movie> GetMovieByIdAsync(string id);
        Task<List<Movie>> GetMoviesByYearAsync(int year);
        Task<List<Movie>> GetMoviesByYearsAsync(int[] years);
        Task<List<Movie>> GetMoviesInRangeAsync(DateTime start, DateTime end);
        Task AddMovieAsync(Movie movie);
        Task UpdateMovieAsync(Movie movie);
        Task DeleteMovieAsync(string id);
        Task DeleteMoviesAsync(IEnumerable<string> ids);
        Task SaveChangesAsync();
    }
}
