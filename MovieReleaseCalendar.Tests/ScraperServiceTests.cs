using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MovieReleaseCalendar.API.Models;
using MovieReleaseCalendar.API.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System;
using System.Linq;

namespace MovieReleaseCalendar.Tests
{
    // In-memory test repository for IMovieRepository
    public class TestMovieRepository : IMovieRepository
    {
        private readonly Dictionary<string, Movie> _movies = new();
        public Task<Movie> GetMovieByIdAsync(string id) => Task.FromResult(_movies.TryGetValue(id, out var m) ? m : null);
        public Task EnsureDatabaseReadyAsync() => Task.CompletedTask;
        public Task<bool> HasMoviesAsync() => Task.FromResult(_movies.Count > 0);
        public Task<List<Movie>> GetMoviesByYearAsync(int year) => Task.FromResult(new List<Movie>(_movies.Values.Where(m => m.ReleaseDate.Year == year)));
        public Task<List<Movie>> GetMoviesByYearsAsync(int[] years) => Task.FromResult(new List<Movie>(_movies.Values.Where(m => years.Contains(m.ReleaseDate.Year))));
        public Task<List<Movie>> GetMoviesInRangeAsync(DateTime start, DateTime end) => Task.FromResult(new List<Movie>(_movies.Values.Where(m => m.ReleaseDate >= start && m.ReleaseDate <= end).OrderBy(m => m.ReleaseDate)));
        public Task<List<Movie>> GetAllMoviesAsync() => Task.FromResult(new List<Movie>(_movies.Values));
        public Task AddMovieAsync(Movie movie) { _movies[movie.Id] = movie; return Task.CompletedTask; }
        public Task UpdateMovieAsync(Movie movie) { _movies[movie.Id] = movie; return Task.CompletedTask; }
        public Task DeleteMovieAsync(string id) { _movies.Remove(id); return Task.CompletedTask; }
        public Task DeleteMoviesAsync(IEnumerable<string> ids) { foreach (var id in ids) _movies.Remove(id); return Task.CompletedTask; }
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    public class ScraperServiceTests
    {
        // Helper classes must be defined before their usage in test methods
        public class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;
            public FakeHttpMessageHandler(HttpResponseMessage response) => _response = response;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(_response);
        }

        public class DelegatingHandlerStub : DelegatingHandler
        {
            private readonly Queue<HttpResponseMessage> _responses;
            public DelegatingHandlerStub(Queue<HttpResponseMessage> responses)
            {
                _responses = responses;
            }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_responses.Count > 0 ? _responses.Dequeue() : new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }
        }

        public class TestableScraperService : ScraperService
        {
            public TestableScraperService(IMovieRepository repo, ILogger<ScraperService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
                : base(repo, logger, httpClientFactory, configuration) { }

            public virtual new List<HtmlAgilityPack.HtmlNode> CollectAnchorGroup(ref HtmlAgilityPack.HtmlNode node) => base.CollectAnchorGroup(ref node);
            public virtual new bool IsStruckThrough(HtmlAgilityPack.HtmlNode anchor) => base.IsStruckThrough(anchor);
            public virtual new (string, string, string) ExtractTitles(HtmlAgilityPack.HtmlNode anchor) => base.ExtractTitles(anchor);
            public virtual new string NormalizeLink(HtmlAgilityPack.HtmlNode anchor) => base.NormalizeLink(anchor);
            public virtual new DateTime? GetDateFromTag(HtmlAgilityPack.HtmlNode tag, int year) => base.GetDateFromTag(tag, year);
            public virtual new bool NeedsUpdate(Movie movie) => base.NeedsUpdate(movie);
            public virtual new Task<List<TmDbGenre>> LoadGenresAsync(CancellationToken cancellationToken = default) => base.LoadGenresAsync(cancellationToken);
            public virtual new Task<(string, string)> GetMovieCreditsFromTmDbAsync(int movieId, CancellationToken cancellationToken = default) => base.GetMovieCreditsFromTmDbAsync(movieId, cancellationToken);
            public virtual new Task<(int, string, List<string>, string)> GetMovieDetailsFromTmdbAsync(string title, DateTime releaseDate, List<TmDbGenre> genres, CancellationToken cancellationToken = default) => base.GetMovieDetailsFromTmdbAsync(title, releaseDate, genres, cancellationToken);
            public virtual new Task<Movie> BuildNewMovieAsync(string title, string cleanTitle, DateTime releaseDate, string fullLink, string id, List<TmDbGenre> genres, CancellationToken cancellationToken = default) => base.BuildNewMovieAsync(title, cleanTitle, releaseDate, fullLink, id, genres, cancellationToken);
            public virtual new Task UpdateExistingMovieAsync(Movie movie, string cleanTitle, DateTime releaseDate, string fullLink, List<TmDbGenre> genres, CancellationToken cancellationToken = default) => base.UpdateExistingMovieAsync(movie, cleanTitle, releaseDate, fullLink, genres, cancellationToken);
            public virtual new Task DeleteNonExistingMovies(int[] years, HashSet<string> seen) => base.DeleteNonExistingMovies(years, seen);
            public virtual new Task ProcessMovieTitlesAsync(HtmlAgilityPack.HtmlNode tag, DateTime releaseDate, HashSet<string> seen, List<Movie> results, List<TmDbGenre> genres, CancellationToken cancellationToken = default) => base.ProcessMovieTitlesAsync(tag, releaseDate, seen, results, genres, cancellationToken);
        }

        private readonly Mock<ILogger<ScraperService>> _loggerMock = new();
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
        private readonly Mock<IConfiguration> _configurationMock = new();

        private ScraperService CreateService(IMovieRepository repo, HttpClient httpClient = null, string apiKey = "fake-key")
        {
            _httpClientFactoryMock.Reset();
            _configurationMock.Reset();
            _configurationMock.Setup(c => c["TMDb:ApiKey"]).Returns(apiKey);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient ?? new HttpClient());
            return new ScraperService(
                repo,
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _configurationMock.Object
            );
        }
        private TestableScraperService CreateTestableService(IMovieRepository repo, HttpClient httpClient = null, string apiKey = "fake-key")
        {
            _httpClientFactoryMock.Reset();
            _configurationMock.Reset();
            _configurationMock.Setup(c => c["TMDb:ApiKey"]).Returns(apiKey);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient ?? new HttpClient());
            return new TestableScraperService(
                repo,
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _configurationMock.Object
            );
        }

        [Fact]
        public async Task ScrapeAsync_ReturnsEmptyList_WhenApiKeyMissing()
        {
            var repo = new TestMovieRepository();
            var service = CreateService(repo, apiKey: null);

            // Act
            var result = await service.ScrapeAsync(new int[0]);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ScrapeAsync_LogsWarning_WhenApiKeyMissing()
        {
            var repo = new TestMovieRepository();
            var service = CreateService(repo, apiKey: null);

            // Act
            await service.ScrapeAsync(new int[0]);

            // Assert
            _loggerMock.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString() != null && v.ToString().Contains("No years provided for scraping. Returning empty results.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task ScrapeAsync_ReturnsEmptyList_WhenTmdbApiFails()
        {
            var repo = new TestMovieRepository();
            var fakeResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };
            var httpClient = new HttpClient(new FakeHttpMessageHandler(fakeResponse));
            var service = CreateService(repo, httpClient: httpClient);

            // Act
            var result = await service.ScrapeAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ScrapeAsync_ReturnsEmptyList_WhenHtmlFetchFails()
        {
            var repo = new TestMovieRepository();
            var fakeTmdbResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"genres\":[]}", System.Text.Encoding.UTF8, "application/json")
            };
            var fakeHtmlResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("", System.Text.Encoding.UTF8, "text/html")
            };
            var handlerQueue = new Queue<HttpResponseMessage>(new[] { fakeTmdbResponse, fakeHtmlResponse });
            var httpClient = new HttpClient(new DelegatingHandlerStub(handlerQueue));
            var service = CreateService(repo, httpClient: httpClient);

            // Act
            var result = await service.ScrapeAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ScrapeAsync_ReturnsMovie_WhenAllExternalCallsSucceed()
        {
            var repo = new TestMovieRepository();
            var fakeTmdbResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"genres\":[{\"id\":1,\"name\":\"Action\"}]}" , System.Text.Encoding.UTF8, "application/json")
            };
            int year = 2024;
            // Use a valid HTML structure for the Scraper
            var html = "<h4><strong>January 24</strong> (Friday)</h4><p style='margin-top:2px' class='sched'><a class='showTip' href='//example.com/' data-url='//example.com/test.jpg'><strong>Test Movie</strong></a><br />";
            var fakeHtmlResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html")
            };
            // Provide enough TMDb movie responses for all alternative title lookups
            var fakeTmdbMovieResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"results\":[{\"title\":\"Test Movie\",\"overview\":\"A test movie.\",\"genre_ids\":[1],\"poster_path\":\"/poster.jpg\",\"release_date\":\"2024-01-24\"}],\"total_results\":1}", System.Text.Encoding.UTF8, "application/json")
            };
            var handlerQueue = new Queue<HttpResponseMessage>(new[] {
                fakeTmdbResponse, // genres
                fakeHtmlResponse, // HTML
                fakeTmdbMovieResponse, // movie search 1
                fakeTmdbMovieResponse, // movie search 2 (alt title)
                fakeTmdbMovieResponse  // movie search 3 (alt title)
            });
            var httpClient = new HttpClient(new DelegatingHandlerStub(handlerQueue));
            var service = CreateService(repo, httpClient: httpClient);
            var result = await service.ScrapeAsync(new[] { year });
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Test Movie", result[0].Title);
            Assert.Equal("A test movie.\nhttps://example.com/", result[0].Description.Trim());
            Assert.Equal("Action", result[0].Genres[0]);
        }

        [Fact]
        public async Task ScrapeAsync_DeletesMoviesNotInSource()
        {
            var repo = new TestMovieRepository();
            var fakeTmdbResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"genres\":[{\"id\":1,\"name\":\"Action\"}]}" , System.Text.Encoding.UTF8, "application/json")
            };
            int year = 2024;
            var htmlYear1 = "<h4><strong>January 24</strong> (Friday)</h4><p style='margin-top:2px' class='sched'><a class='showTip' href='//example.com/' data-url='//example.com/test.jpg'><strong>Movie One</strong></a><br />";
            var htmlYear2 = "<h4><strong>January 24</strong> (Friday)</h4><p style='margin-top:2px' class='sched'></p>";
            var fakeHtmlResponse1 = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(htmlYear1, System.Text.Encoding.UTF8, "text/html")
            };
            var fakeHtmlResponse2 = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(htmlYear2, System.Text.Encoding.UTF8, "text/html")
            };
            var fakeTmdbMovieResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"results\":[{\"title\":\"Movie One\",\"overview\":\"Desc\",\"genre_ids\":[1],\"poster_path\":\"/poster.jpg\",\"release_date\":\"2024-01-24\"}],\"total_results\":1}", System.Text.Encoding.UTF8, "application/json")
            };
            // First scrape: movie is present
            var handlerQueue1 = new Queue<HttpResponseMessage>(new[] {
                fakeTmdbResponse, fakeHtmlResponse1,
                fakeTmdbMovieResponse, fakeTmdbMovieResponse, fakeTmdbMovieResponse
            });
            var httpClient1 = new HttpClient(new DelegatingHandlerStub(handlerQueue1));
            var service1 = CreateService(repo, httpClient: httpClient1);
            var result1 = await service1.ScrapeAsync(new[] { year });
            Assert.Single(result1);
            // Second scrape: movie is missing, should be deleted
            var handlerQueue2 = new Queue<HttpResponseMessage>(new[] { fakeTmdbResponse, fakeHtmlResponse2 });
            var httpClient2 = new HttpClient(new DelegatingHandlerStub(handlerQueue2));
            var service2 = CreateService(repo, httpClient: httpClient2);
            var result2 = await service2.ScrapeAsync(new[] { year });
            Assert.Empty(result2);
        }

        [Fact]
        public async Task ScrapeAsync_DoesNotAddDuplicateMovies()
        {
            var repo = new TestMovieRepository();
            var fakeTmdbResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"genres\":[{\"id\":1,\"name\":\"Action\"}]}" , System.Text.Encoding.UTF8, "application/json")
            };
            var html = "<h4><strong>January 24</strong> (Friday)</h4><p style='margin-top:2px' class='sched'><a class='showTip' href='//example.com/' data-url='//example.com/test.jpg'><strong>Movie One</strong></a><a class='showTip' href='//example.com/' data-url='//example.com/test.jpg'><strong>Movie One</strong></a><br />";
            var fakeHtmlResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html")
            };
            var fakeTmdbMovieResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"results\":[{\"title\":\"Movie One\",\"overview\":\"Desc\",\"genre_ids\":[1],\"poster_path\":\"/poster.jpg\",\"release_date\":\"2024-01-24\"}],\"total_results\":1}", System.Text.Encoding.UTF8, "application/json")
            };
            var handlerQueue = new Queue<HttpResponseMessage>(new[] { fakeTmdbResponse, fakeHtmlResponse, fakeTmdbMovieResponse, fakeTmdbMovieResponse, fakeTmdbMovieResponse });
            var httpClient = new HttpClient(new DelegatingHandlerStub(handlerQueue));
            var service = CreateService(repo, httpClient: httpClient);
            var result = await service.ScrapeAsync(new[] { 2024 });
            Assert.Single(result);
        }

        [Fact]
        public async Task BuildNewMovieAsync_SetsScrapedAtToRecentValue()
        {
            var service = CreateTestableService(new TestMovieRepository(), apiKey: null);
            var before = DateTimeOffset.UtcNow.AddSeconds(-5);
            var result = await service.BuildNewMovieAsync("Test", "Test", new DateTime(2024, 6, 1), "https://example.com", "test_2024-06-01", new List<TmDbGenre>());
            var after = DateTimeOffset.UtcNow.AddSeconds(5);
            Assert.True(result.ScrapedAt >= before && result.ScrapedAt <= after);
        }

        [Fact]
        public async Task ScrapeAsync_LogsInformation_WhenMovieAddedOrDeleted()
        {
            var repo = new TestMovieRepository();
            var fakeTmdbResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"genres\":[{\"id\":1,\"name\":\"Action\"}]}" , System.Text.Encoding.UTF8, "application/json")
            };
            var html = "<h4><strong>January 24</strong> (Friday)</h4><p style='margin-top:2px' class='sched'><a class='showTip' href='//example.com/' data-url='//example.com/test.jpg'><strong>Movie One</strong></a><br />";
            var fakeHtmlResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html")
            };
            var fakeTmdbMovieResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"results\":[{\"title\":\"Movie One\",\"overview\":\"Desc\",\"genre_ids\":[1],\"poster_path\":\"/poster.jpg\",\"release_date\":\"2024-01-24\"}],\"total_results\":1}", System.Text.Encoding.UTF8, "application/json")
            };
            var handlerQueue = new Queue<HttpResponseMessage>(new[] { fakeTmdbResponse, fakeHtmlResponse, fakeTmdbMovieResponse });
            var httpClient = new HttpClient(new DelegatingHandlerStub(handlerQueue));
            var service = CreateService(repo, httpClient: httpClient);
            await service.ScrapeAsync(new[] { 2024 });
            _loggerMock.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("Stored: Movie One")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task ScrapeAsync_MapsGenreIdsToNames()
        {
            var repo = new TestMovieRepository();
            var fakeTmdbResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"genres\":[{\"id\":1,\"name\":\"Action\"},{\"id\":2,\"name\":\"Comedy\"}]}" , System.Text.Encoding.UTF8, "application/json")
            };
            int year = 2024;
            var html = "<h4><strong>January 24</strong> (Friday)</h4><p style='margin-top:2px' class='sched'><a class='showTip' href='//example.com/' data-url='//example.com/test.jpg'><strong>Test Movie</strong></a><br />";
            var fakeHtmlResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html")
            };
            var fakeTmdbMovieResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"results\":[{\"title\":\"Test Movie\",\"overview\":\"A test movie.\",\"genre_ids\":[1,2],\"poster_path\":\"/poster.jpg\",\"release_date\":\"2024-06-01\"}],\"total_results\":1}", System.Text.Encoding.UTF8, "application/json")
            };
            var handlerQueue = new Queue<HttpResponseMessage>(new[] {
                fakeTmdbResponse, fakeHtmlResponse,
                fakeTmdbMovieResponse, fakeTmdbMovieResponse, fakeTmdbMovieResponse
            });
            var httpClient = new HttpClient(new DelegatingHandlerStub(handlerQueue));
            var service = CreateService(repo, httpClient: httpClient);

            // Act
            var result = await service.ScrapeAsync(new[] { year });

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Test Movie", result[0].Title);
            Assert.Contains("Action", result[0].Genres);
            Assert.Contains("Comedy", result[0].Genres);
        }

        // Example: Mocking LoadGenresAsync in a test using an override
        [Fact]
        public async Task LoadGenresAsync_CanBeMockedInTest()
        {
            var repo = new TestMovieRepository();
            var service = new TestableScraperService(repo, _loggerMock.Object, _httpClientFactoryMock.Object, _configurationMock.Object)
            {
                // Override LoadGenresAsync for this test
                // C# syntax: override in a derived class
            };
            var mockService = new MockedScraperService(repo, _loggerMock.Object, _httpClientFactoryMock.Object, _configurationMock.Object);
            var result = await mockService.LoadGenresAsync();
            Assert.Single(result);
            Assert.Equal("MockGenre", result[0].Name);
        }

        // Inline derived class for mocking
        public class MockedScraperService : TestableScraperService
        {
            public MockedScraperService(IMovieRepository repo, ILogger<ScraperService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
                : base(repo, logger, httpClientFactory, configuration) { }
            public override Task<List<TmDbGenre>> LoadGenresAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(new List<TmDbGenre> { new TmDbGenre { Id = 99, Name = "MockGenre" } });
        }

        [Theory]
        [InlineData("<a><strong>Title</strong></a>", false)]
        [InlineData("<a><strong><s>Title</s></strong></a>", true)]
        [InlineData("<a>Title</a>", true)]
        public void IsStruckThrough_DetectsStrikethrough(string html, bool expected)
        {
            var service = CreateTestableService(new TestMovieRepository());
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var anchor = doc.DocumentNode.SelectSingleNode("//a");
            Assert.Equal(expected, service.IsStruckThrough(anchor));
        }

        [Fact]
        public void CollectAnchorGroup_ReturnsMultipleAnchors()
        {
            var service = CreateTestableService(new TestMovieRepository());
            var html = "<p class='sched'><a href='1'>A</a><a href='2'>B</a><br /></p>";
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var node = doc.DocumentNode.SelectSingleNode("//a");
            var result = service.CollectAnchorGroup(ref node);
            Assert.Equal(2, result.Count);
            Assert.Equal("a", result[0].Name);
            Assert.Equal("a", result[1].Name);
        }

        [Theory]
        [InlineData("Test Movie [2024]", "Test Movie", "testmovie")]
        [InlineData("Another Title", "Another Title", "anothertitle")]
        public void ExtractTitles_NormalizesCorrectly(string raw, string clean, string normalized)
        {
            var service = CreateTestableService(new TestMovieRepository());
            var html = $"<a>{raw}</a>";
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var anchor = doc.DocumentNode.SelectSingleNode("//a");
            var (r, c, n) = service.ExtractTitles(anchor);
            Assert.Equal(raw, r);
            Assert.Equal(clean, c);
            Assert.Equal(normalized, n);
        }

        [Theory]
        [InlineData("//example.com", "https://example.com")]
        [InlineData("https://foo.com", "https://foo.com")]
        [InlineData("/relative", "/relative")]
        public void NormalizeLink_HandlesVariousLinks(string href, string expected)
        {
            var service = CreateTestableService(new TestMovieRepository());
            var html = $"<a href='{href}'>Test</a>";
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var anchor = doc.DocumentNode.SelectSingleNode("//a");
            Assert.Equal(expected, service.NormalizeLink(anchor));
        }

        [Theory]
        [InlineData("<h4><strong>June 1</strong></h4>", 2024, "2024-06-01")]
        [InlineData("<h4><strong>Invalid</strong></h4>", 2024, null)]
        public void GetDateFromTag_ParsesOrReturnsNull(string html, int year, string expected)
        {
            var service = CreateTestableService(new TestMovieRepository());
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var tag = doc.DocumentNode.SelectSingleNode("//h4");
            var result = service.GetDateFromTag(tag, year);
            if (expected == null)
                Assert.Null(result);
            else
                Assert.Equal(DateTime.Parse(expected), result);
        }

        [Theory]
        [InlineData("desc", "poster", new[] { "Action" }, false)]
        [InlineData("", "poster", new[] { "Action" }, true)]
        [InlineData("desc", "", new[] { "Action" }, true)]
        [InlineData("desc", "poster", new string[0], true)]
        public void NeedsUpdate_ReturnsExpected(string desc, string poster, string[] genres, bool expected)
        {
            var service = CreateTestableService(new TestMovieRepository());
            var movie = new Movie { Description = desc, PosterUrl = poster, Genres = genres == null ? null : new List<string>(genres) };
            Assert.Equal(expected, service.NeedsUpdate(movie));
        }

        [Fact]
        public async Task LoadGenresAsync_ReturnsEmptyOnTmdbError()
        {
            var service = CreateTestableService(new TestMovieRepository(), httpClient: new HttpClient(new FakeHttpMessageHandler(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError))));
            var result = await service.LoadGenresAsync();
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetMovieDetailsFromTmdbAsync_ReturnsDefaultsOnTmdbError()
        {
            var service = CreateTestableService(new TestMovieRepository(), httpClient: new HttpClient(new FakeHttpMessageHandler(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError))));
            var result = await service.GetMovieDetailsFromTmdbAsync("Test", new DateTime(2024, 6, 1), new List<TmDbGenre>());
            Assert.Equal(0, result.Item1);
            Assert.Equal("No description available", result.Item2);
            Assert.Empty(result.Item3);
            Assert.Equal(string.Empty, result.Item4);
        }

        [Fact]
        public async Task DeleteNonExistingMovies_DeletesAllAndNone()
        {
            var repo = new TestMovieRepository();
            var movie1 = new Movie { Id = "m1", ReleaseDate = new DateTime(2024, 1, 1) };
            var movie2 = new Movie { Id = "m2", ReleaseDate = new DateTime(2024, 1, 1) };
            await repo.AddMovieAsync(movie1);
            await repo.AddMovieAsync(movie2);
            await repo.SaveChangesAsync();
            var service = CreateTestableService(repo);
            // Delete all
            await service.DeleteNonExistingMovies(new[] { 2024 }, new HashSet<string>());
            var all = await repo.GetAllMoviesAsync();
            Assert.Empty(all);
            // Add back and delete none
            await repo.AddMovieAsync(movie1);
            await repo.SaveChangesAsync();
            await service.DeleteNonExistingMovies(new[] { 2024 }, new HashSet<string> { "m1" });
            all = await repo.GetAllMoviesAsync();
            Assert.Single(all);
        }

        [Fact]
        public async Task ProcessMovieTitlesAsync_SkipsStruckThrough()
        {
            var repo = new TestMovieRepository();
            var service = CreateTestableService(repo);
            var html = "<p class='sched'><a class='showTip'><strong><s>Skip Me</s></strong></a><a class='showTip'><strong>Keep Me</strong></a><br /></p>";
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var tag = doc.DocumentNode.SelectSingleNode("//p");
            var seen = new HashSet<string>();
            var results = new List<Movie>();
            var date = new DateTime(2024, 1, 1);
            await service.ProcessMovieTitlesAsync(tag, date, seen, results, new List<TmDbGenre>());
            Assert.Empty(results);
        }
    }
}
