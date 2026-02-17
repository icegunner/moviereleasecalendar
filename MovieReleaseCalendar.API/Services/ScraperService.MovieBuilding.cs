using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
    public partial class ScraperService
    {
        protected async Task<Movie> BuildNewMovieAsync(string title, string cleanTitle, DateTime releaseDate, string fullLink, string id, List<TmDbGenre> genres, CancellationToken cancellationToken = default)
        {
            var tmdbDetails = await GetMovieDetailsFromTmdbAsync(cleanTitle, releaseDate, genres, cancellationToken);

            var castList = new List<string>();
            var directorsList = new List<string>();
            string imdbId = string.Empty;
            string mpaaRating = string.Empty;
            var trailers = new List<TrailerLink>();

            if (tmdbDetails.Id != 0)
            {
                // Parallelize the independent TMDb follow-up calls
                var creditsTask = GetMovieCreditsFromTmDbAsync(tmdbDetails.Id, cancellationToken);
                var imdbTask = GetImdbIdAsync(tmdbDetails.Id, cancellationToken);
                var ratingTask = GetMpaaRatingAsync(tmdbDetails.Id, cancellationToken);
                var trailersTask = GetTrailersAsync(tmdbDetails.Id, cancellationToken);

                await Task.WhenAll(creditsTask, imdbTask, ratingTask, trailersTask);

                var tmdbCredits = creditsTask.Result;
                castList = tmdbCredits.Cast;
                directorsList = tmdbCredits.Directors;
                imdbId = imdbTask.Result;
                mpaaRating = ratingTask.Result;
                trailers = trailersTask.Result;
            }

            var castDisplay = castList.Count > 0 ? string.Join(", ", castList) : string.Empty;
            var directorDisplay = directorsList.Count > 0 ? string.Join(", ", directorsList) : string.Empty;

            return new Movie
            {
                Id = id,
                Title = title,
                ReleaseDate = releaseDate,
                Url = fullLink,
                Description = $"{tmdbDetails.Description}{(string.IsNullOrEmpty(castDisplay) ? "" : $"\nStarring: {castDisplay}.")}{(string.IsNullOrEmpty(directorDisplay) ? "" : $"\nDirected by: {directorDisplay}.")}\n{fullLink}",
                Genres = tmdbDetails.Genres,
                PosterUrl = tmdbDetails.PosterUrl,
                TmdbId = tmdbDetails.Id,
                ImdbId = imdbId,
                Directors = directorsList,
                Cast = castList,
                MpaaRating = mpaaRating,
                Trailers = trailers,
                ScrapedAt = DateTimeOffset.UtcNow
            };
        }

        protected async Task UpdateExistingMovieAsync(Movie movie, string cleanTitle, DateTime releaseDate, string fullLink, List<TmDbGenre> genres, CancellationToken cancellationToken = default)
        {
            var tmdbDetails = await GetMovieDetailsFromTmdbAsync(cleanTitle, releaseDate, genres, cancellationToken);

            var castList = new List<string>();
            var directorsList = new List<string>();
            string imdbId = string.Empty;
            string mpaaRating = string.Empty;
            var trailers = new List<TrailerLink>();

            if (tmdbDetails.Id != 0)
            {
                var creditsTask = GetMovieCreditsFromTmDbAsync(tmdbDetails.Id, cancellationToken);
                var imdbTask = GetImdbIdAsync(tmdbDetails.Id, cancellationToken);
                var ratingTask = GetMpaaRatingAsync(tmdbDetails.Id, cancellationToken);
                var trailersTask = GetTrailersAsync(tmdbDetails.Id, cancellationToken);

                await Task.WhenAll(creditsTask, imdbTask, ratingTask, trailersTask);

                var tmdbCredits = creditsTask.Result;
                castList = tmdbCredits.Cast;
                directorsList = tmdbCredits.Directors;
                imdbId = imdbTask.Result;
                mpaaRating = ratingTask.Result;
                trailers = trailersTask.Result;
            }

            var castDisplay = castList.Count > 0 ? string.Join(", ", castList) : string.Empty;
            var directorDisplay = directorsList.Count > 0 ? string.Join(", ", directorsList) : string.Empty;

            movie.Description = $"{tmdbDetails.Description}{(string.IsNullOrEmpty(castDisplay) ? "" : $"\nStarring: {castDisplay}.")}{(string.IsNullOrEmpty(directorDisplay) ? "" : $"\nDirected by: {directorDisplay}.")}\n{fullLink}";
            movie.Genres = tmdbDetails.Genres;
            movie.PosterUrl = tmdbDetails.PosterUrl;
            movie.TmdbId = tmdbDetails.Id;
            movie.ImdbId = imdbId;
            movie.Directors = directorsList;
            movie.Cast = castList;
            movie.MpaaRating = mpaaRating;
            movie.Trailers = trailers;
            movie.ScrapedAt = DateTimeOffset.UtcNow;
        }

        protected bool NeedsUpdate(Movie movie)
        {
            return string.IsNullOrWhiteSpace(movie.Description) ||
                   movie.Description == "No description available" ||
                   string.IsNullOrWhiteSpace(movie.PosterUrl) ||
                   movie.Genres == null || !movie.Genres.Any() ||
                   movie.TmdbId == 0 ||
                   string.IsNullOrWhiteSpace(movie.ImdbId) ||
                   (movie.Directors == null || !movie.Directors.Any()) ||
                   (movie.Cast == null || !movie.Cast.Any()) ||
                   string.IsNullOrWhiteSpace(movie.MpaaRating) ||
                   movie.Trailers == null || !movie.Trailers.Any();
        }

        protected async Task<List<TmDbGenre>> LoadGenresAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_tmdbApiKey))
            {
                _logger.LogWarning("TMDb API key is not configured. Skipping genre lookup.");
                return new List<TmDbGenre>();
            }

            if (_tmdbDisabled)
            {
                return new List<TmDbGenre>();
            }

            try
            {
                var response = await MakeApiCall<TmDbGenreResponse>("https://api.themoviedb.org/3/genre/movie/list?language=en", cancellationToken: cancellationToken);
                if (_tmdbDisabled || response.Result == null)
                {
                    return new List<TmDbGenre>();
                }
                return response.Result.Genres ?? new List<TmDbGenre>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load genres from TMDb.");
                return new List<TmDbGenre>();
            }
        }

        protected async Task<(List<string> Cast, List<string> Directors)> GetMovieCreditsFromTmDbAsync(int movieId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_tmdbApiKey) || _tmdbDisabled)
            {
                _logger.LogWarning("TMDb API key is either not configured or has problems. Skipping TMDb lookup.");
                return (new List<string>(), new List<string>());
            }

            try
            {
                var tmdbUrl = $"https://api.themoviedb.org/3/movie/{movieId}/credits?api_key={_tmdbApiKey}";
                var tmdbResponse = await MakeApiCall<TmdbCreditsResponse>(tmdbUrl, cancellationToken: cancellationToken);

                if (tmdbResponse.Result == null)
                {
                    _logger.LogDebug($"No credits found for movie ID {movieId}");
                    return (new List<string>(), new List<string>());
                }

                var cast = tmdbResponse.Result.Cast.Take(5).Select(c => c.Name).ToList();
                var directors = tmdbResponse.Result.Crew.Where(c => c.Job == "Director").Select(c => c.Name).ToList();

                return (cast, directors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch credits for movie ID {movieId} from TMDb.");
                return (new List<string>(), new List<string>());
            }
        }

        /// <summary>
        /// Fetches the IMDB ID for a movie from TMDb using the /movie/{id} endpoint.
        /// </summary>
        protected async Task<string> GetImdbIdAsync(int tmdbId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_tmdbApiKey) || _tmdbDisabled)
            {
                return string.Empty;
            }

            try
            {
                var url = $"https://api.themoviedb.org/3/movie/{tmdbId}?api_key={_tmdbApiKey}";
                var response = await MakeApiCall<TmdbMovieDetailResponse>(url, cancellationToken: cancellationToken);

                return response.Result?.ImdbId ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch IMDB ID for TMDb movie {tmdbId}.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Fetches the US theatrical MPAA rating for a movie from TMDb using the /movie/{id}/release_dates endpoint.
        /// Looks for US (iso_3166_1 == "US") theatrical release (type == 3) certification.
        /// Falls back to type 2 (limited theatrical) if no type 3 found.
        /// </summary>
        protected async Task<string> GetMpaaRatingAsync(int tmdbId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_tmdbApiKey) || _tmdbDisabled)
            {
                return string.Empty;
            }

            try
            {
                var url = $"https://api.themoviedb.org/3/movie/{tmdbId}/release_dates?api_key={_tmdbApiKey}";
                var response = await MakeApiCall<TmdbReleaseDatesResponse>(url, cancellationToken: cancellationToken);

                if (response.Result?.Results == null)
                {
                    return string.Empty;
                }

                var usRelease = response.Result.Results.FirstOrDefault(r => r.Iso3166_1 == "US");
                if (usRelease == null)
                {
                    return string.Empty;
                }

                // Prefer type 3 (Theatrical), fall back to type 2 (Theatrical limited)
                var theatrical = usRelease.ReleaseDates.FirstOrDefault(r => r.Type == 3 && !string.IsNullOrEmpty(r.Certification))
                              ?? usRelease.ReleaseDates.FirstOrDefault(r => r.Type == 2 && !string.IsNullOrEmpty(r.Certification));

                return theatrical?.Certification ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch MPAA rating for TMDb movie {tmdbId}.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Fetches US English trailers and teasers from TMDb /movie/{id}/videos endpoint.
        /// Returns full URLs for known sites (YouTube, Vimeo).
        /// </summary>
        protected async Task<List<TrailerLink>> GetTrailersAsync(int tmdbId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_tmdbApiKey) || _tmdbDisabled)
            {
                return new List<TrailerLink>();
            }

            try
            {
                var url = $"https://api.themoviedb.org/3/movie/{tmdbId}/videos?api_key={_tmdbApiKey}";
                var response = await MakeApiCall<TmdbVideosResponse>(url, cancellationToken: cancellationToken);

                if (response.Result?.Results == null)
                {
                    return new List<TrailerLink>();
                }

                var trailers = new List<TrailerLink>();
                foreach (var video in response.Result.Results)
                {
                    // Only US English trailers and teasers
                    if (video.Iso639_1 != "en" || video.Iso3166_1 != "US")
                        continue;
                    if (video.Type != "Trailer" && video.Type != "Teaser")
                        continue;

                    var fullUrl = BuildVideoUrl(video.Site, video.Key);
                    if (fullUrl == null)
                        continue;

                    trailers.Add(new TrailerLink
                    {
                        Name = video.Name,
                        Url = fullUrl,
                        Site = video.Site,
                        PublishedAt = video.PublishedAt
                    });
                }

                // Sort oldest first
                trailers.Sort((a, b) => (a.PublishedAt ?? DateTimeOffset.MaxValue).CompareTo(b.PublishedAt ?? DateTimeOffset.MaxValue));

                return trailers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch trailers for TMDb movie {tmdbId}.");
                return new List<TrailerLink>();
            }
        }

        private static string BuildVideoUrl(string site, string key)
        {
            switch (site)
            {
                case "YouTube": return $"https://www.youtube.com/watch?v={key}";
                case "Vimeo": return $"https://vimeo.com/{key}";
                default: return null;
            }
        }

        protected async Task<(int Id, string Description, List<string> Genres, string PosterUrl)> GetMovieDetailsFromTmdbAsync(string title, DateTime releaseDate, List<TmDbGenre> genres, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_tmdbApiKey) || _tmdbDisabled)
            {
                _logger.LogWarning("TMDb API key is either not configured or has problems. Skipping TMDb lookup.");
                return (0, "No description available", new List<string>(), string.Empty);
            }

            var titlesToTry = GetAlternativeTitles(title);
            try
            {
                foreach (var t in titlesToTry)
                {
                    var searchResult = await TmdbSearchAsync<TmDbResponse>(t, releaseDate.Year, cancellationToken);
                    if (_tmdbDisabled || searchResult.Result == null)
                    {
                        return (0, "No description available", new List<string>(), string.Empty);
                    }
                    if (searchResult.Result.TotalResults > 0)
                    {
                        var tmdbMovie = searchResult.Result.Movies.First();
                        var description = string.IsNullOrEmpty(tmdbMovie.Overview) ? "No description available" : tmdbMovie.Overview;
                        var movieGenres = tmdbMovie.GenreIds.Select(id => genres.FirstOrDefault(s => s.Id == id)?.Name ?? id.ToString()).ToList();
                        var posterUrl = !string.IsNullOrEmpty(tmdbMovie.PosterPath) ? $"https://image.tmdb.org/t/p/w500{tmdbMovie.PosterPath}" : string.Empty;
                        return (tmdbMovie.Id, description, movieGenres, posterUrl);
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
