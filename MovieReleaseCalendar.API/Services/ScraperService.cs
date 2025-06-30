using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Models;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
    public class ScraperService : IScraperService
    {
        private readonly IDocumentStore _store;
        private readonly ILogger<ScraperService> _logger;
        private readonly HttpClient _client;
        private readonly string _tmdbApiKey;

        private List<TmDbGenre> _genres;

        public ScraperService(IDocumentStore store, ILogger<ScraperService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _store = store;
            _logger = logger;
            _client = httpClientFactory.CreateClient();
            _tmdbApiKey = Environment.GetEnvironmentVariable("TMDB_APIKEY") ?? configuration["TMDb:ApiKey"];
        }

        public async Task<List<Movie>> ScrapeAsync()
        {
            var years = new[] { DateTime.UtcNow.Year - 1, DateTime.UtcNow.Year, DateTime.UtcNow.Year + 1 };
            var results = new List<Movie>();
            var seen = new HashSet<string>();
            _genres = await LoadGenresAsync();

            using var session = _store.OpenAsyncSession();

            foreach (var year in years)
            {
                var html = await TryFetchHtmlForYearAsync(year);
                if (string.IsNullOrEmpty(html)) continue;

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var tags = doc.DocumentNode.SelectNodes("//h4|//p");
                if (tags == null) continue;

                DateTime? releaseDate = null;

                _logger.LogTrace($"Traversing HTML nodes for year {year}.");
                _logger.LogTrace($"Found {tags.Count(s => s.Name == "h4")} \"h4\" (release dates) and {tags.Count(s => s.Name == "p" && s.GetClasses().Contains("sched"))} \"p\" (titles) tags.");
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
                        await ProcessMovieTitlesAsync(tag, releaseDate.Value, session, seen, results);
                    }
                }
                await session.SaveChangesAsync();
            }

            await DeleteNonExistingMovies(session, years, seen);

            return results;
        }

        protected async Task DeleteNonExistingMovies(IAsyncDocumentSession session, int[] years, HashSet<string> seen)
        {
            _logger.LogInformation($"Looking for movie titles stored that no longer exist on firstshowing.net for years: {string.Join(", ", years)}");
            var allExisting = await session.Query<Movie>().ToListAsync();
            var allExistingIds = allExisting
                .Where(m => years.Contains(m.ReleaseDate.Year))
                .Select(m => m.Id);
            _logger.LogDebug($"Found {allExistingIds.Count()} existing movie IDs for years: {string.Join(", ", years)}");

            var idsToDelete = allExistingIds.Except(seen).ToList();
            _logger.LogInformation($"Deleting {idsToDelete.Count} movie title(s) that no longer exist on firstshowing.net (removed, renamed, etc.).");
            foreach (var id in idsToDelete)
            {
                session.Delete(id);
                _logger.LogDebug($"Deleted movie with ID: {id} (no longer present on source site)");
            }
            await session.SaveChangesAsync();
            _logger.LogInformation($"Finished deleting non-existing movies for years: {string.Join(", ", years)}");
        }

        protected async Task ProcessMovieTitlesAsync(HtmlNode tag, DateTime releaseDate, IAsyncDocumentSession session, HashSet<string> seen, List<Movie> results)
        {
            var node = tag.FirstChild;

            while (node != null)
            {
                var aGroup = CollectAnchorGroup(ref node);
                if (aGroup.Count == 0 || IsStruckThrough(aGroup[0])) continue;

                var (title, cleanTitle, normalizedTitle) = ExtractTitles(aGroup[0]);
                var fullLink = NormalizeLink(aGroup[0]);
                var key = $"{normalizedTitle}_{releaseDate:yyyy-MM-dd}";

                if (!seen.Add(key)) continue;

                var existing = await session.Query<Movie>()
                    .Where(m => m.Id == key)
                    .FirstOrDefaultAsync();

                if (existing == null)
                {
                    var movie = await BuildNewMovieAsync(title, cleanTitle, releaseDate, fullLink, key);
                    results.Add(movie);
                    await session.StoreAsync(movie);
                    _logger.LogInformation($"Stored: {title} on {releaseDate:yyyy-MM-dd}");
                }
                else if (NeedsUpdate(existing))
                {
                    await UpdateExistingMovieAsync(existing, cleanTitle, releaseDate, fullLink);
                    _logger.LogInformation($"Updated existing movie: {title} on {releaseDate:yyyy-MM-dd}");
                }
            }
        }

        #region HTML Helper Methods
        protected async Task<string> TryFetchHtmlForYearAsync(int year)
        {
            var url = $"https://www.firstshowing.net/schedule{year}";
            _logger.LogInformation($"Fetching: {url}");

            try
            {
                return await _client.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to download year {year}");
                return null;
            }
        }

        protected List<HtmlNode> CollectAnchorGroup(ref HtmlNode node)
        {
            var group = new List<HtmlNode>();
            var checkNode = node;
            var ignoreNode = false;

            while (checkNode != null && checkNode.Name != "br")
            {
                if (checkNode.InnerHtml.ToLower().Contains("expands") || checkNode.InnerHtml.ToLower().Contains("re-release"))
                {
                    ignoreNode = true;
                }
                checkNode = checkNode.NextSibling;
            }

            while (node != null && node.Name != "br")
            {
                if (node.Name == "a" && !ignoreNode)
                    group.Add(node);

                node = node.NextSibling;
            }

            if (node?.Name == "br")
                node = node.NextSibling;

            return group;
        }

        protected bool IsStruckThrough(HtmlNode anchor)
        {
            var strong = anchor.SelectSingleNode(".//strong");
            return strong == null || strong.SelectSingleNode(".//s") != null;
        }

        protected (string RawTitle, string CleanTitle, string NormalizedTitle) ExtractTitles(HtmlNode anchor)
        {
            var rawTitle = anchor.InnerText.Trim();
            var cleanTitle = Regex.Replace(rawTitle, @"\s*\[.*?\]\s*$", "");
            var normalizedTitle = NormalizeTitle(cleanTitle);
            return (rawTitle, cleanTitle, normalizedTitle);
        }

        protected string NormalizeTitle(string title)
        {
            string normalized = title.Normalize(NormalizationForm.FormD); // Normalize Unicode to decompose accents
            // Remove diacritics
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            string withoutDiacritics = sb.ToString().Normalize(NormalizationForm.FormC);
            string cleaned = Regex.Replace(withoutDiacritics, @"[^a-zA-Z0-9_]", ""); // Remove all non-alphanumeric and non-underscore characters
            return cleaned.ToLower();
        }

        protected string NormalizeLink(HtmlNode anchor)
        {
            var link = anchor.GetAttributeValue("href", string.Empty).Trim();
            return link.StartsWith("//") ? $"https:{link}" : link;
        }

        protected DateTime? GetDateFromTag(HtmlNode tag, int year)
        {
            var strong = tag.SelectSingleNode(".//strong");
            if (strong == null) return null;

            var text = strong.InnerText.Trim();
            try
            {
                return DateTime.ParseExact($"{text}, {year}", "MMMM d, yyyy", CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                _logger.LogDebug($"Failed to parse date: {text}");
                return null;
            }
        }
        #endregion HTML Helper Methods

        #region Movie Building and Updating
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
                Description = $"{tmdbDetails.Description}\nStarring: {tmdbCredits.Cast}.\nDirected by: {tmdbCredits.Director}.\n{fullLink}",
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

            movie.Description = $"{tmdbDetails.Description}\nStarring: {tmdbCredits.Cast}.\nDirected by: {tmdbCredits.Director}.\n{fullLink}";
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

        private List<string> GetAlternativeTitles(string title)
        {
            var titles = new List<string> { title };

            // Remove anything after colon or dash
            var baseTitle = Regex.Replace(title, @"\s*[:\-]\s*.*$", "");
            if (baseTitle != title) titles.Add(baseTitle);

            // Remove possessives, HTML entities, and after colon/dash
            var superCleanTitle = Regex.Replace(title, @"(\b[\p{L}\p{M}\p{N}\.]+(?:\s+[\p{L}\p{M}\p{N}\.]+)*'s?\s+)|(&#\d+;)|(\s*[:\-]\s*.*$)", "").TrimEnd();
            if (superCleanTitle != baseTitle && superCleanTitle != title) titles.Add(superCleanTitle);

            return titles.Distinct().ToList();
        }
        #endregion Movie Building and Updating

        protected async Task<(TmDbResponse Result, HttpResponseMessage Response)> TmdbSearchAsync<T>(string title, int year)
        {
            var tmdbUrl = $"https://api.themoviedb.org/3/search/movie?query={Uri.EscapeDataString(title)}&language=en-US&year={year}";
            return await MakeApiCall<TmDbResponse>(tmdbUrl);
        }

        private async Task<(T Result, HttpResponseMessage Response)> MakeApiCall<T>(string requestUri, AuthenticationHeaderValue authHeader = null, HttpMethod method = null, HttpContent content = null, bool includeResponse = false)
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
                response = await _client.SendAsync(request);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Error making request to API.\nRequest URI: {requestUri}\nException:\n{ex.Message}");
                throw;
            }

            if (!response.IsSuccessStatusCode)
            {
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
