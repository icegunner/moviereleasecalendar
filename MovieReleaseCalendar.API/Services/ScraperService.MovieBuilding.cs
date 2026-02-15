using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
    public partial class ScraperService
    {
        protected async Task<Movie> BuildNewMovieAsync(string title, string cleanTitle, DateTime releaseDate, string fullLink, string id)
        {
            var tmdbDetails = await GetMovieDetailsFromTmdbAsync(cleanTitle, releaseDate);
            (string Cast, string Director) tmdbCredits = (string.Empty, string.Empty);
            if (tmdbDetails.Id != 0)
            {
                tmdbCredits = await GetMovieCreditsFromTmDbAsync(tmdbDetails.Id);
            }

            return new Movie
            {
                Id = id,
                Title = title,
                ReleaseDate = releaseDate,
                Url = fullLink,
                Description = $"{tmdbDetails.Description}{(string.IsNullOrEmpty(tmdbCredits.Cast) ? "" : $"\nStarring: {tmdbCredits.Cast}.")}{(string.IsNullOrEmpty(tmdbCredits.Director) ? "" : "\nDirected by: {tmdbCredits.Director}.")}\n{fullLink}",
                Genres = tmdbDetails.Genres,
                PosterUrl = tmdbDetails.PosterUrl,
                ScrapedAt = DateTimeOffset.UtcNow
            };
        }

        protected async Task UpdateExistingMovieAsync(Movie movie, string cleanTitle, DateTime releaseDate, string fullLink)
        {
            var tmdbDetails = await GetMovieDetailsFromTmdbAsync(cleanTitle, releaseDate);
            (string Cast, string Director) tmdbCredits = (string.Empty, string.Empty);
            if (tmdbDetails.Id != 0)
            {
                tmdbCredits = await GetMovieCreditsFromTmDbAsync(tmdbDetails.Id);
            }

            movie.Description = $"{tmdbDetails.Description}\nStarring: {(string.IsNullOrEmpty(tmdbCredits.Cast) ? "Unknown" : tmdbCredits.Cast)}.\nDirected by: {(string.IsNullOrEmpty(tmdbCredits.Director) ? "Unknown" : tmdbCredits.Director)}.\n{fullLink}";
            movie.Genres = tmdbDetails.Genres;
            movie.PosterUrl = tmdbDetails.PosterUrl;
        }

        protected bool NeedsUpdate(Movie movie)
        {
            return string.IsNullOrWhiteSpace(movie.Description) ||
                   movie.Description == "No description available" ||
                   string.IsNullOrWhiteSpace(movie.PosterUrl) ||
                   movie.Genres == null || !movie.Genres.Any();
        }

        protected async Task<List<TmDbGenre>> LoadGenresAsync()
        {
            if (string.IsNullOrEmpty(_tmdbApiKey))
            {
                _logger.LogWarning("TMDb API key is not configured. Skipping genre lookup.");
                return new List<TmDbGenre>();
            }

            try
            {
                var response = await MakeApiCall<TmDbGenreResponse>("https://api.themoviedb.org/3/genre/movie/list?language=en");
                return response.Result.Genres;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load genres from TMDb.");
                return new List<TmDbGenre>();
            }
        }

        protected async Task<(string Cast, string Director)> GetMovieCreditsFromTmDbAsync(int movieId)
        {
            if (string.IsNullOrEmpty(_tmdbApiKey))
            {
                _logger.LogWarning("TMDb API key is not configured. Skipping TMDb lookup.");
                return (string.Empty, string.Empty);
            }

            try
            {
                var tmdbUrl = $"https://api.themoviedb.org/3/movie/{movieId}/credits?api_key={_tmdbApiKey}";
                var tmdbResponse = await MakeApiCall<TmdbCreditsResponse>(tmdbUrl);

                if (tmdbResponse.Result == null)
                {
                    _logger.LogDebug($"No credits found for movie ID {movieId}");
                    return (string.Empty, string.Empty);
                }

                var cast = string.Join(", ", tmdbResponse.Result.Cast.Take(5).Select(c => c.Name));
                var director = string.Join(", ", tmdbResponse.Result.Crew.Where(c => c.Job == "Director").Select(c => c.Name));

                return (cast, director);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch credits for movie ID {movieId} from TMDb.");
                return (string.Empty, string.Empty);
            }
        }

        protected async Task<(int Id, string Description, List<string> Genres, string PosterUrl)> GetMovieDetailsFromTmdbAsync(string title, DateTime releaseDate)
        {
            if (string.IsNullOrEmpty(_tmdbApiKey))
            {
                _logger.LogWarning("TMDb API key is not configured. Skipping TMDb lookup.");
                return (0, "No description available", new List<string>(), string.Empty);
            }

            var titlesToTry = GetAlternativeTitles(title);
            try
            {
                foreach (var t in titlesToTry)
                {
                    var searchResult = await TmdbSearchAsync<TmDbResponse>(t, releaseDate.Year);
                    if (searchResult.Result.TotalResults > 0)
                    {
                        var tmdbMovie = searchResult.Result.Movies.First();
                        var description = string.IsNullOrEmpty(tmdbMovie.Overview) ? "No description available" : tmdbMovie.Overview;
                        var genres = tmdbMovie.GenreIds.Select(id => _genres.FirstOrDefault(s => s.Id == id)?.Name ?? id.ToString()).ToList();
                        var posterUrl = !string.IsNullOrEmpty(tmdbMovie.PosterPath) ? $"https://image.tmdb.org/t/p/w500{tmdbMovie.PosterPath}" : string.Empty;
                        return (tmdbMovie.Id, description, genres, posterUrl);
                    }
                    _logger.LogDebug($"No results found for \"{t}\" ({releaseDate.Year}).");
                }

                _logger.LogDebug($"No results found for any title variant of \"{title}\" ({releaseDate.Year}).");
                return (0, "No description available", new List<string>(), string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch details for {title} ({releaseDate.Year}) from TMDb.");
                return (0, "No description available", new List<string>(), string.Empty);
            }
        }
    }
}
