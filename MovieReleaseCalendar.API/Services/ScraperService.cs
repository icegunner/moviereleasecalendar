using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
    public partial class ScraperService : IScraperService
    {
        private readonly IMovieRepository _movieRepository;
        private readonly ILogger<ScraperService> _logger;
        private readonly HttpClient _client;
        private readonly string _tmdbApiKey;
        private bool _tmdbDisabled;

        public ScraperService(IMovieRepository movieRepository, ILogger<ScraperService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _movieRepository = movieRepository;
            _logger = logger;
            _client = httpClientFactory.CreateClient();
            _tmdbApiKey = Environment.GetEnvironmentVariable("TMDB_APIKEY") ?? configuration["TMDb:ApiKey"];
        }

        public async Task<ScrapeResult> ScrapeAsync(CancellationToken cancellationToken = default)
        {
            var years = new[] { DateTime.UtcNow.Year - 1, DateTime.UtcNow.Year, DateTime.UtcNow.Year + 1 };
            return await ScrapeAsync(years, cancellationToken);
        }

        public async Task<ScrapeResult> ScrapeAsync(IEnumerable<int> years, CancellationToken cancellationToken = default)
        {
            var yearArray = years?.ToArray() ?? Array.Empty<int>();
            var result = new ScrapeResult();

            if (yearArray.Length == 0)
            {
                _logger.LogWarning("No years provided for scraping. Returning empty results.");
                return result;
            }

            var seen = new HashSet<string>();
            var genres = await LoadGenresAsync(cancellationToken);

            foreach (var year in yearArray)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var html = await TryFetchHtmlForYearAsync(year, cancellationToken);
                if (string.IsNullOrEmpty(html)) continue;

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var tags = doc.DocumentNode.SelectNodes("//h4|//p");
                if (tags == null) continue;

                DateTime? releaseDate = null;

                _logger.LogTrace($"Traversing HTML nodes for year {year}.");
                _logger.LogTrace($"Found {tags.Count(s => s.Name == "h4")}" +
                    " \"h4\" (release dates) and " +
                    $"{tags.Count(s => s.Name == "p" && s.GetClasses().Contains("sched"))} \"p\" (titles) tags.");
                foreach (var tag in tags.Where(t => t.Name == "h4" || (t.Name == "p" && t.GetClasses().Contains("sched"))))
                {
                    if (tag.Name == "h4" && tag.FirstChild.Name == "a")
                    {
                        // This is the last H4 tag containing links to next and previous years, skip it
                        _logger.LogInformation($"Finished traversing nodes for year {year}.");
                        continue;
                    }

                    if (tag.Name == "h4")
                    {
                        releaseDate = GetDateFromTag(tag, year);
                    }
                    else if (tag.Name == "p" && tag.GetClasses().Contains("sched") && releaseDate != null)
                    {
                        await ProcessMovieTitlesAsync(tag, releaseDate.Value, seen, result, genres, cancellationToken);
                    }
                }
            }

            await DeleteNonExistingMovies(yearArray, seen);

            return result;
        }

        protected async Task DeleteNonExistingMovies(int[] years, HashSet<string> seen)
        {
            _logger.LogInformation($"Looking for movie titles stored that no longer exist on firstshowing.net for years: {string.Join(", ", years)}");
            var allExisting = await _movieRepository.GetMoviesByYearsAsync(years);
            var allExistingIds = allExisting.Select(m => m.Id);
            _logger.LogDebug($"Found {allExistingIds.Count()} existing movie IDs for years: {string.Join(", ", years)}");

            var idsToDelete = allExistingIds.Except(seen).ToList();
            _logger.LogInformation($"Deleting {idsToDelete.Count} movie title(s) that no longer exist on firstshowing.net (removed, renamed, etc.).");
            if (idsToDelete.Count > 0)
            {
                await _movieRepository.DeleteMoviesAsync(idsToDelete);
                foreach (var id in idsToDelete)
                {
                    _logger.LogDebug($"Deleted movie with ID: {id} (no longer present on source site)");
                }
            }
            _logger.LogInformation($"Finished deleting non-existing movies for years: {string.Join(", ", years)}");
        }

        protected async Task ProcessMovieTitlesAsync(HtmlNode tag, DateTime releaseDate, HashSet<string> seen, ScrapeResult result, List<TmDbGenre> genres, CancellationToken cancellationToken = default)
        {
            var node = tag.FirstChild;

            while (node != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var aGroup = CollectAnchorGroup(ref node);
                if (aGroup.Count == 0 || IsStruckThrough(aGroup[0])) continue;

                var (title, cleanTitle, normalizedTitle) = ExtractTitles(aGroup[0]);
                var fullLink = NormalizeLink(aGroup[0]);
                var key = $"{normalizedTitle}_{releaseDate:yyyy-MM-dd}";

                if (!seen.Add(key)) continue;

                var existing = await _movieRepository.GetMovieByIdAsync(key);

                if (existing == null)
                {
                    var movie = await BuildNewMovieAsync(title, cleanTitle, releaseDate, fullLink, key, genres, cancellationToken);
                    result.NewMovies.Add(movie);
                    await _movieRepository.AddMovieAsync(movie);
                    _logger.LogInformation($"Stored: {title} on {releaseDate:yyyy-MM-dd}");
                }
                else if (NeedsUpdate(existing))
                {
                    await UpdateExistingMovieAsync(existing, cleanTitle, releaseDate, fullLink, genres, cancellationToken);
                    result.UpdatedMovies.Add(existing);
                    await _movieRepository.UpdateMovieAsync(existing);
                    _logger.LogInformation($"Updated existing movie: {title} on {releaseDate:yyyy-MM-dd}");
                }
            }
        }

        // HTML Helper Methods moved to ScraperService.HtmlHelpers.cs
        // Movie Building and Updating methods moved to ScraperService.MovieBuilding.cs

        protected async Task<(TmDbResponse Result, HttpResponseMessage Response)> TmdbSearchAsync<T>(string title, int year, CancellationToken cancellationToken = default)
        {
            var tmdbUrl = $"https://api.themoviedb.org/3/search/movie?query={Uri.EscapeDataString(title)}&language=en-US&year={year}";
            return await MakeApiCall<TmDbResponse>(tmdbUrl, cancellationToken: cancellationToken);
        }

        private async Task<(T Result, HttpResponseMessage Response)> MakeApiCall<T>(string requestUri, AuthenticationHeaderValue authHeader = null, HttpMethod method = null, HttpContent content = null, bool includeResponse = false, CancellationToken cancellationToken = default)
        {
            method ??= HttpMethod.Get;
            authHeader ??= new AuthenticationHeaderValue("Bearer", _tmdbApiKey);

            var request = new HttpRequestMessage(method, requestUri);
            if (content != null)
                request.Content = content;

            request.Headers.Authorization = authHeader;
            HttpResponseMessage response;
            try
            {
                response = await _client.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Error making request to API.\nRequest URI: {requestUri}\nException:\n{ex.Message}");
                throw;
            }

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                if (statusCode == 401 || statusCode == 403)
                {
                    _logger.LogError($"TMDb API returned {statusCode} ({response.ReasonPhrase}). The API key may be invalid, expired, or unauthorized. All further TMDb calls will be skipped for this scrape run.");
                    _tmdbDisabled = true;
                }
                else
                {
                    _logger.LogWarning($"TMDb API returned {statusCode} ({response.ReasonPhrase}) for request: {requestUri}");
                }

                if (!includeResponse)
                    response.Dispose();
                return (default, includeResponse ? response : null);
            }

            var contentToDeserialize = await response.Content.ReadAsStringAsync();

            try
            {
                var result = JsonConvert.DeserializeObject<T>(contentToDeserialize);
                return (result, includeResponse ? response : null);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"Failed to deserialize response body into {typeof(T).Name}.\nRequest URI: {requestUri}\nRaw response:\n{contentToDeserialize}");
                return (default, includeResponse ? response : null);
            }
        }
    }
}
