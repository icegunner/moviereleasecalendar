using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Raven.Client.Documents;
using Raven.TestDriver;
using MovieReleaseCalendar.API.Models;
using MovieReleaseCalendar.API.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System;

namespace MovieReleaseCalendar.Tests
{
    public class ScraperServiceTests : RavenTestDriver
    {
        private readonly Mock<ILogger<ScraperService>> _loggerMock = new();
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
        private readonly Mock<IConfiguration> _configurationMock = new();

        private ScraperService CreateService(IDocumentStore store, HttpClient httpClient = null, string apiKey = "fake-key")
        {
            _httpClientFactoryMock.Reset();
            _configurationMock.Reset();
            _configurationMock.Setup(c => c["TMDb:ApiKey"]).Returns(apiKey);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient ?? new HttpClient());
            return new ScraperService(
                store,
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _configurationMock.Object
            );
        }

        [Fact]
        public async Task ScrapeAsync_ReturnsEmptyList_WhenApiKeyMissing()
        {
            using var store = GetDocumentStore();
            var service = CreateService(store, apiKey: null);

            // Act
            var result = await service.ScrapeAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ScrapeAsync_LogsWarning_WhenApiKeyMissing()
        {
            using var store = GetDocumentStore();
            var service = CreateService(store, apiKey: null);

            // Act
            await service.ScrapeAsync();

            // Assert
            _loggerMock.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString() != null && v.ToString().Contains("TMDb API key is not configured.")),
				null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task ScrapeAsync_ReturnsEmptyList_WhenTmdbApiFails()
        {
            using var store = GetDocumentStore();
            var fakeResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };
            var httpClient = new HttpClient(new FakeHttpMessageHandler(fakeResponse));
            var service = CreateService(store, httpClient: httpClient);

            // Act
            var result = await service.ScrapeAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ScrapeAsync_ReturnsEmptyList_WhenHtmlFetchFails()
        {
            using var store = GetDocumentStore();
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
            var service = CreateService(store, httpClient: httpClient);

            // Act
            var result = await service.ScrapeAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ScrapeAsync_ReturnsMovie_WhenAllExternalCallsSucceed()
        {
            using var store = GetDocumentStore();
            var fakeTmdbResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"genres\":[{\"id\":1,\"name\":\"Action\"}]}", System.Text.Encoding.UTF8, "application/json")
            };
            // Minimal HTML for a single movie
            var html = @"<h4><strong>June 1</strong></h4><p class='sched'><a href='https://example.com'>Test Movie</a><br /></p>";
            var fakeHtmlResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html")
            };
            // TMDb movie search response
            var fakeTmdbMovieResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"results\":[{\"title\":\"Test Movie\",\"overview\":\"A test movie.\",\"genre_ids\":[1],\"poster_path\":\"/poster.jpg\",\"release_date\":\"2024-06-01\"}],\"total_results\":1}", System.Text.Encoding.UTF8, "application/json")
            };
            var handlerQueue = new Queue<HttpResponseMessage>(new[] { fakeTmdbResponse, fakeHtmlResponse, fakeTmdbMovieResponse });
            var httpClient = new HttpClient(new DelegatingHandlerStub(handlerQueue));
            var service = CreateService(store, httpClient: httpClient);

            // Act
            var result = await service.ScrapeAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Test Movie", result[0].Title);
            Assert.Equal("A test movie.", result[0].Description);
            Assert.Equal("Action", result[0].Genres[0]);
        }

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

        // Helper subclass to expose and allow mocking of protected methods for testing
        public class TestableScraperService : ScraperService
        {
            public TestableScraperService(IDocumentStore store, ILogger<ScraperService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
                : base(store, logger, httpClientFactory, configuration) { }

            public virtual new List<HtmlAgilityPack.HtmlNode> CollectAnchorGroup(ref HtmlAgilityPack.HtmlNode node) => base.CollectAnchorGroup(ref node);
            public virtual new bool IsStruckThrough(HtmlAgilityPack.HtmlNode anchor) => base.IsStruckThrough(anchor);
            public virtual (string, string, string) ExtractTitles(HtmlAgilityPack.HtmlNode anchor) => base.ExtractTitles(anchor);
            public virtual new string NormalizeLink(HtmlAgilityPack.HtmlNode anchor) => base.NormalizeLink(anchor);
            public virtual new DateTime? GetDateFromTag(HtmlAgilityPack.HtmlNode tag, int year) => base.GetDateFromTag(tag, year);
            public virtual new bool NeedsUpdate(Movie movie) => base.NeedsUpdate(movie);
            public virtual new Task<List<TmDbGenre>> LoadGenresAsync() => base.LoadGenresAsync();
            public virtual new Task<(string, string)> GetMovieCreditsFromTmDbAsync(int movieId) => base.GetMovieCreditsFromTmDbAsync(movieId);
            public virtual new Task<(int, string, List<string>, string)> GetMovieDetailsFromTmdbAsync(string title, DateTime releaseDate) => base.GetMovieDetailsFromTmdbAsync(title, releaseDate);
            public virtual new Task<Movie> BuildNewMovieAsync(string title, string cleanTitle, DateTime releaseDate, string fullLink, string id) => base.BuildNewMovieAsync(title, cleanTitle, releaseDate, fullLink, id);
            public virtual new Task UpdateExistingMovieAsync(Movie movie, string cleanTitle, DateTime releaseDate, string fullLink) => base.UpdateExistingMovieAsync(movie, cleanTitle, releaseDate, fullLink);
        }

        private TestableScraperService CreateTestableService(IDocumentStore store, HttpClient httpClient = null, string apiKey = "fake-key")
        {
            _httpClientFactoryMock.Reset();
            _configurationMock.Reset();
            _configurationMock.Setup(c => c["TMDb:ApiKey"]).Returns(apiKey);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient ?? new HttpClient());
            return new TestableScraperService(
                store,
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _configurationMock.Object
            );
        }

        [Fact]
        public void CollectAnchorGroup_ReturnsAnchors()
        {
            var service = CreateTestableService(GetDocumentStore());
            var html = "<p class='sched'><a href='https://example.com'>Test</a><br /></p>";
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var node = doc.DocumentNode.SelectSingleNode("//a");
            var result = service.CollectAnchorGroup(ref node);
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("a", result[0].Name);
        }

        [Fact]
        public void IsStruckThrough_ReturnsFalseForNormalAnchor()
        {
            var service = CreateTestableService(GetDocumentStore());
            var html = "<a>Test</a>";
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var anchor = doc.DocumentNode.SelectSingleNode("//a");
            Assert.NotNull(anchor);
            var result = service.IsStruckThrough(anchor);
            Assert.False(result);
        }

        [Fact]
        public void ExtractTitles_ReturnsRawCleanAndNormalized()
        {
            var service = CreateTestableService(GetDocumentStore());
            var html = "<a>Test Movie [2024]</a>";
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var anchor = doc.DocumentNode.SelectSingleNode("//a");
            Assert.NotNull(anchor);
            var (raw, clean, normalized) = service.ExtractTitles(anchor);
            Assert.Equal("Test Movie [2024]", raw);
            Assert.Equal("Test Movie", clean);
            Assert.Equal("testmovie", normalized); // Normalized removes spaces and brackets, lowercases
        }

        [Fact]
        public void NormalizeLink_HandlesDoubleSlash()
        {
            var service = CreateTestableService(GetDocumentStore());
            var html = "<a href='//example.com'>Test</a>";
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var anchor = doc.DocumentNode.SelectSingleNode("//a");
            Assert.NotNull(anchor);
            var result = service.NormalizeLink(anchor);
            Assert.StartsWith("https://", result);
        }

        [Fact]
        public void GetDateFromTag_ParsesDate()
        {
            var service = CreateTestableService(GetDocumentStore());
            var html = "<h4><strong>June 1, 2024</strong></h4>";
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var tag = doc.DocumentNode.SelectSingleNode("//h4");
            Assert.NotNull(tag);
            var result = service.GetDateFromTag(tag, 2024);
            Assert.Equal(new DateTime(2024, 6, 1), result);
        }

        [Fact]
        public void NeedsUpdate_ReturnsTrueForEmptyMovie()
        {
            var service = CreateTestableService(GetDocumentStore());
            var movie = new Movie();
            var result = service.NeedsUpdate(movie);
            Assert.True(result);
        }

        [Fact]
        public async Task LoadGenresAsync_ReturnsEmptyList_WhenApiKeyMissing()
        {
            var service = CreateTestableService(GetDocumentStore(), apiKey: null);
            var result = await service.LoadGenresAsync();
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetMovieCreditsFromTmDbAsync_ReturnsEmpty_WhenApiKeyMissing()
        {
            var service = CreateTestableService(GetDocumentStore(), apiKey: null);
            var result = await service.GetMovieCreditsFromTmDbAsync(123);
            Assert.Equal((string.Empty, string.Empty), result);
        }

        [Fact]
        public async Task GetMovieDetailsFromTmdbAsync_ReturnsDefaults_WhenApiKeyMissing()
        {
            var service = CreateTestableService(GetDocumentStore(), apiKey: null);
            var result = await service.GetMovieDetailsFromTmdbAsync("Test", new DateTime(2024, 6, 1));
            Assert.Equal(0, result.Item1);
            Assert.Equal("No description available", result.Item2);
            Assert.Empty(result.Item3);
            Assert.Equal(string.Empty, result.Item4);
        }

        [Fact]
        public async Task BuildNewMovieAsync_ReturnsMovie()
        {
            var service = CreateTestableService(GetDocumentStore(), apiKey: null);
            var result = await service.BuildNewMovieAsync("Test", "Test", new DateTime(2024, 6, 1), "https://example.com", "test_2024-06-01");
            Assert.Equal("Test", result.Title);
            Assert.Equal(new DateTime(2024, 6, 1), result.ReleaseDate);
        }

        [Fact]
        public async Task UpdateExistingMovieAsync_DoesNotThrow()
        {
            var service = CreateTestableService(GetDocumentStore(), apiKey: null);
            var movie = new Movie { Title = "Test", ReleaseDate = new DateTime(2024, 6, 1) };
            await service.UpdateExistingMovieAsync(movie, "Test", new DateTime(2024, 6, 1), "https://example.com");
            Assert.NotNull(movie);
        }

        [Fact]
        public async Task ScrapeAsync_MapsGenreIdsToNames()
        {
            using var store = GetDocumentStore();
            var fakeTmdbResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"genres\":[{\"id\":1,\"name\":\"Action\"},{\"id\":2,\"name\":\"Comedy\"}]}", System.Text.Encoding.UTF8, "application/json")
            };
            var html = @"<h4><strong>June 1</strong></h4><p class='sched'><a href='https://example.com'>Test Movie</a><br /></p>";
            var fakeHtmlResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html")
            };
            var fakeTmdbMovieResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"results\":[{\"title\":\"Test Movie\",\"overview\":\"A test movie.\",\"genre_ids\":[1,2],\"poster_path\":\"/poster.jpg\",\"release_date\":\"2024-06-01\"}],\"total_results\":1}", System.Text.Encoding.UTF8, "application/json")
            };
            var handlerQueue = new Queue<HttpResponseMessage>(new[] { fakeTmdbResponse, fakeHtmlResponse, fakeTmdbMovieResponse });
            var httpClient = new HttpClient(new DelegatingHandlerStub(handlerQueue));
            var service = CreateService(store, httpClient: httpClient);

            // Act
            var result = await service.ScrapeAsync();

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
            var store = GetDocumentStore();
            var service = new TestableScraperService(store, _loggerMock.Object, _httpClientFactoryMock.Object, _configurationMock.Object)
            {
                // Override LoadGenresAsync for this test
                // C# syntax: override in a derived class
            };
            var mockService = new MockedScraperService(store, _loggerMock.Object, _httpClientFactoryMock.Object, _configurationMock.Object);
            var result = await mockService.LoadGenresAsync();
            Assert.Single(result);
            Assert.Equal("MockGenre", result[0].Name);
        }

        // Inline derived class for mocking
        public class MockedScraperService : TestableScraperService
        {
            public MockedScraperService(IDocumentStore store, ILogger<ScraperService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
                : base(store, logger, httpClientFactory, configuration) { }
            public override Task<List<TmDbGenre>> LoadGenresAsync()
                => Task.FromResult(new List<TmDbGenre> { new TmDbGenre { Id = 99, Name = "MockGenre" } });
        }
    }
}
