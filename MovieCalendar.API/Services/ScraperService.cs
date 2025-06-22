
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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _tmdbApiKey;

        private static readonly int[] YEARS = [DateTime.UtcNow.Year - 1, DateTime.UtcNow.Year, DateTime.UtcNow.Year + 1];

        public ScraperService(IDocumentStore store, ILogger<ScraperService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _store = store;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _tmdbApiKey = configuration["TMDb:ApiKey"] ?? string.Empty;
        }

        public async Task<List<Movie>> ScrapeAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var results = new List<Movie>();
            var seen = new HashSet<string>();

            var genreResponse = MakeApiCall<TmDbGenreResponse>(client, _tmdbApiKey, "https://api.themoviedb.org/3/genre/movie/list?language=en").Result;
            var genreList = genreResponse.Result.Genres;

            using var session = _store.OpenAsyncSession();

            foreach (var year in YEARS)
            {
                var url = $"https://www.firstshowing.net/schedule{year}";
                _logger.LogInformation($"Fetching: {url}");

                string html;
                try
                {
                    html = client.GetStringAsync(url).Result;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to download year {year}");
                    continue;
                }

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                DateTime? currentDate = null;
                var tags = doc.DocumentNode.SelectNodes("//h4|//p");

                if (tags == null) continue;

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

                                try
                                {
                                    var tmdbUrl = $"https://api.themoviedb.org/3/search/movie?query={Uri.EscapeDataString(cleanTitle)}&year={currentDate.Value.Year}";
                                    var tmdbResponse = await MakeApiCall<TmDbResponse>(client, _tmdbApiKey, tmdbUrl);

                                    if (tmdbResponse.Result.TotalResults == 0)
                                    {
                                        var baseTitle = Regex.Replace(cleanTitle, @"\s*[:\-]\s*.*$", "");
                                        _logger.LogDebug($"Failed to lookup {cleanTitle}. Trying {baseTitle} instead.");
                                        tmdbUrl = $"https://api.themoviedb.org/3/search/movie?query={Uri.EscapeDataString(baseTitle)}&year={currentDate.Value.Year}";
                                        tmdbResponse = await MakeApiCall<TmDbResponse>(client, _tmdbApiKey, tmdbUrl);
                                        if (tmdbResponse.Result.TotalResults > 0)
                                        {
                                            _logger.LogDebug($"Found {baseTitle}.");
                                        }
                                    }

                                    tmdbMovie = tmdbResponse.Result.Movies.First();

                                    description = tmdbMovie.Overview;
                                    genres = genreList.Where(s => tmdbMovie.GenreIds.Contains(s.Id)).Select(s => s.Name).ToList();
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"TMDb lookup failed for: {title}");
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

        private async Task<(T Result, HttpResponseMessage Response)> MakeApiCall<T>(HttpClient client, string apiToken, string requestUri, AuthenticationHeaderValue authHeader = null, HttpMethod method = null, HttpContent content = null, bool includeResponse = false)
        {
            method ??= HttpMethod.Get;
            authHeader ??= new AuthenticationHeaderValue("Bearer", apiToken);

            var request = new HttpRequestMessage(method, requestUri);
            if (content != null)
                request.Content = content;

            request.Headers.Authorization = authHeader;
            HttpResponseMessage response = null;

            try
            {
                response = await client.SendAsync(request);
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

            if (typeof(T) == typeof(string))
            {
                // Cast to object and then to T to satisfy the return type
                return ((T)(object)contentToDeserialize, includeResponse ? response : null);
            }

            try
            {
                var result = JsonConvert.DeserializeObject<T>(contentToDeserialize);
                return (result, includeResponse ? response : null);
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Failed to deserialize response body into {typeof(T).Name}.\nRequest URI: {requestUri}\nRaw response:\n{contentToDeserialize}");
                return (default, includeResponse ? response : null);
            }
        }
    }
}
