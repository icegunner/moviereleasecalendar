using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using MovieCalendar.API.Data;
using MovieCalendar.API.Models;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace MovieCalendar.API.Services
{
    public class ScraperService
    {
        private readonly IDocumentStore _store;
        private readonly ILogger<ScraperService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string SourceUrl = "https://www.firstshowing.net/some-path/"; // Replace with actual

        public ScraperService(IDocumentStore store, ILogger<ScraperService> logger, IHttpClientFactory httpClientFactory)
        {
            _store = store;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<Movie>> ScrapeAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var html = await client.GetStringAsync(SourceUrl);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var scrapedMovies = new List<Movie>();
            var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'release-listing')]");

            if (nodes == null)
            {
                _logger.LogWarning("No release listings found on the page.");
                return scrapedMovies;
            }

            using var session = _store.OpenAsyncSession();

            foreach (var node in nodes)
            {
                try
                {
                    var title = node.SelectSingleNode(".//h3")?.InnerText.Trim() ?? "Untitled";
                    var dateStr = node.SelectSingleNode(".//p[@class='date']")?.InnerText.Trim();
                    var url = node.SelectSingleNode(".//a")?.GetAttributeValue("href", "");
                    var description = node.SelectSingleNode(".//p[@class='review']")?.InnerText.Trim();

                    if (!DateTime.TryParse(dateStr, out var releaseDate))
                    {
                        _logger.LogWarning($"Unable to parse release date for movie '{title}'");
                        continue;
                    }

                    var movie = new Movie
                    {
                        Title = title,
                        ReleaseDate = releaseDate,
                        Url = url ?? string.Empty,
                        Description = description
                    };

                    var existing = await session.Query<Movie>()
                        .Where(m => m.Title == movie.Title && m.ReleaseDate == movie.ReleaseDate)
                        .FirstOrDefaultAsync();

                    if (existing == null)
                    {
                        await session.StoreAsync(movie);
                        scrapedMovies.Add(movie);
                        _logger.LogInformation($"Stored new movie: {title} ({releaseDate:yyyy-MM-dd})");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scraping a movie block.");
                }
            }

            await session.SaveChangesAsync();
            return scrapedMovies;
        }
    }
}
