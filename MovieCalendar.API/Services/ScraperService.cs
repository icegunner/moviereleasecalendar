
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MovieCalendar.API.Models;
using Newtonsoft.Json;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MovieCalendar.API.Services
{
    public class ScraperService
    {
        private readonly IDocumentStore _store;
        private readonly ILogger<ScraperService> _logger;
        private readonly HttpClient _client;
        private readonly string _tmdbApiKey;

        private static readonly int[] YEARS = [DateTime.UtcNow.Year - 1, DateTime.UtcNow.Year, DateTime.UtcNow.Year + 1];

        public ScraperService(IDocumentStore store, ILogger<ScraperService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _store = store;
            _logger = logger;
            _client = httpClientFactory.CreateClient();
            _tmdbApiKey = Environment.GetEnvironmentVariable("TMDB_APIKEY") ?? configuration["TMDb:ApiKey"];
        }

        public async Task<List<Movie>> ScrapeAsync()
        {
            var results = new List<Movie>();
            var seen = new HashSet<string>();
            var genreList = new List<TmDbGenre>();

            if (string.IsNullOrEmpty(_tmdbApiKey))
            {
                _logger.LogWarning("TMDb API key is not configured.");
                return results;
            }
            else
            {
                var genreResponse = MakeApiCall<TmDbGenreResponse>("https://api.themoviedb.org/3/genre/movie/list?language=en").Result;
                genreList = genreResponse.Result.Genres;
                _logger.LogDebug($"Loaded {genreList.Count} genres from TMDb.");
            }

            using var session = _store.OpenAsyncSession();

            foreach (var year in YEARS)
            {
                var url = $"https://www.firstshowing.net/schedule{year}";
                _logger.LogInformation($"Fetching: {url}");

                string html;
                try
                {
                    html = _client.GetStringAsync(url).Result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to download year {year}");
                    continue;
                }

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                DateTime? currentDate = null;
                var tags = doc.DocumentNode.SelectNodes("//h4|//p");

                if (tags == null) continue;

                _logger.LogTrace($"Traversing HTML nodes for year {year}.");
                _logger.LogTrace($"Found {tags.Count(s => s.Name == "h4")} \"h4\" and {tags.Count(s => s.Name == "p" && s.GetClasses().Contains("sched"))} \"p\" tags.");
                foreach (var tag in tags)
                {
                    if (tag.Name == "h4")
                    {
                        var strong = tag.SelectSingleNode(".//strong");
                        if (strong != null)
                        {
                            var text = strong.InnerText.Trim();
                            try
                            {
                                currentDate = DateTime.ParseExact($"{text}, {year}", "MMMM d, yyyy", CultureInfo.InvariantCulture);
                            }
                            catch
                            {
                                _logger.LogDebug($"Failed to parse date: {text}, {year}");
                                currentDate = null;
                            }
                        }
                    }
                    else if (tag.Name == "p" && tag.GetClasses().Contains("sched") && currentDate != null)
                    {
                        var node = tag.FirstChild;

                        while (node != null)
                        {
                            // Collect all <a> nodes until the next <br />
                            var aGroup = new List<HtmlNode>();

                            while (node != null && node.Name != "br")
                            {
                                if (node.Name == "a")
                                    aGroup.Add(node);

                                node = node.NextSibling;
                            }

                            // Now node is either <br /> or null, so we advance past the <br />
                            if (node?.Name == "br")
                                node = node.NextSibling;

                            if (aGroup.Count == 0)
                                continue;

                            var firstA = aGroup[0];
                            var titleStrong = firstA.SelectSingleNode(".//strong");

                            // Skip if struck-through
                            if (titleStrong == null || titleStrong != null && titleStrong.SelectSingleNode(".//s") != null)
                                continue;

                            var title = firstA.InnerText.Trim();
                            var cleanTitle = Regex.Replace(title, @"\s*\[.*?\]\s*$", "");
                            var link = firstA.GetAttributeValue("href", string.Empty).Trim();
                            var key = $"{title}|{currentDate:yyyy-MM-dd}";

                            if (!seen.Add(key)) continue;

                            var existing = await session.Query<Movie>()
                                .Where(m => m.Title == title && m.ReleaseDate == currentDate)
                                .FirstOrDefaultAsync();

                            if (existing == null)
                            {
                                var description = "No description available";
                                var genres = new List<string>();
                                var tmdbMovie = new TmDbMovie();

                                if (!string.IsNullOrEmpty(_tmdbApiKey))
                                {
                                    try
                                    {
                                        var tmdbUrl = $"https://api.themoviedb.org/3/search/movie?query={Uri.EscapeDataString(cleanTitle)}&year={currentDate.Value.Year}";
                                        var tmdbResponse = await MakeApiCall<TmDbResponse>(tmdbUrl);

                                        if (tmdbResponse.Result.TotalResults == 0)
                                        {
                                            var baseTitle = Regex.Replace(cleanTitle, @"\s*[:\-]\s*.*$", "");
                                            if (baseTitle == cleanTitle)
                                            {
                                                baseTitle = Regex.Replace(cleanTitle, @"(\b[\p{L}\p{M}\p{N}\.]+(?:\s+[\p{L}\p{M}\p{N}\.]+)*'s?\s+)|(&#\d+;)|(\s*[:\-]\s*.*$)", "").TrimEnd();
                                            }
                                            if (baseTitle != cleanTitle)
                                            {
                                                _logger.LogDebug($"Failed to lookup {cleanTitle}. Trying {baseTitle} instead.");
                                                tmdbUrl = $"https://api.themoviedb.org/3/search/movie?query={Uri.EscapeDataString(baseTitle)}&year={currentDate.Value.Year}";
                                                tmdbResponse = await MakeApiCall<TmDbResponse>(tmdbUrl);
                                                if (tmdbResponse.Result.TotalResults > 0)
                                                {
                                                    _logger.LogDebug($"Found {baseTitle}.");
                                                }
                                                else
                                                {
                                                    var superCleanTitle = Regex.Replace(cleanTitle, @"(\b[\p{L}\p{M}\p{N}\.]+(?:\s+[\p{L}\p{M}\p{N}\.]+)*'s?\s+)|(&#\d+;)|(\s*[:\-]\s*.*$)", "").TrimEnd();
                                                    if (superCleanTitle != baseTitle)
                                                    {
                                                        _logger.LogDebug($"Failed to lookup {baseTitle}. Trying {superCleanTitle} instead.");
                                                        tmdbUrl = $"https://api.themoviedb.org/3/search/movie?query={Uri.EscapeDataString(superCleanTitle)}&year={currentDate.Value.Year}";
                                                        tmdbResponse = MakeApiCall<TmDbResponse>(tmdbUrl).Result;
                                                        if (tmdbResponse.Result.TotalResults > 0)
                                                        {
                                                            _logger.LogDebug($"Found {superCleanTitle}.");
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        tmdbMovie = tmdbResponse.Result.Movies.First();

                                        description = tmdbMovie.Overview;
                                        genres = genreList.Where(s => tmdbMovie.GenreIds.Contains(s.Id)).Select(s => s.Name).ToList();
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, $"TMDb lookup failed for: {title}");
                                    }
                                }
                                else
                                {
                                    _logger.LogDebug("TMDb API key is not configured, skipping TMDb lookup.");
                                }

                                var movie = new Movie
                                {
                                    Title = title,
                                    ReleaseDate = currentDate.Value,
                                    Url = link,
                                    Description = description,
                                    Genres = genres,
                                    PosterUrl = tmdbMovie.PosterPath,
                                    ScrapedAt = DateTimeOffset.UtcNow
                                };

                                results.Add(movie);
                                _logger.LogInformation($"Stored: {title} on {currentDate:yyyy-MM-dd}");
                            }
                        }
                    }
                }
            }

            await session.SaveChangesAsync();
            return results;
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
