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
using Raven.Embedded;
using Raven.Client.Json.Serialization.NewtonsoftJson;
using Newtonsoft.Json.Serialization;
using Raven.Client.Documents.Conventions;

namespace MovieReleaseCalendar.Tests
{
    public class ScraperServiceTests : RavenTestDriver
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
            public TestableScraperService(IDocumentStore store, ILogger<ScraperService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
                : base(store, logger, httpClientFactory, configuration) { }

            public virtual new List<HtmlAgilityPack.HtmlNode> CollectAnchorGroup(ref HtmlAgilityPack.HtmlNode node) => base.CollectAnchorGroup(ref node);
            public virtual new bool IsStruckThrough(HtmlAgilityPack.HtmlNode anchor) => base.IsStruckThrough(anchor);
            public virtual new (string, string, string) ExtractTitles(HtmlAgilityPack.HtmlNode anchor) => base.ExtractTitles(anchor);
            public virtual new string NormalizeLink(HtmlAgilityPack.HtmlNode anchor) => base.NormalizeLink(anchor);
            public virtual new DateTime? GetDateFromTag(HtmlAgilityPack.HtmlNode tag, int year) => base.GetDateFromTag(tag, year);
            public virtual new bool NeedsUpdate(Movie movie) => base.NeedsUpdate(movie);
            public virtual new Task<List<TmDbGenre>> LoadGenresAsync() => base.LoadGenresAsync();
            public virtual new Task<(string, string)> GetMovieCreditsFromTmDbAsync(int movieId) => base.GetMovieCreditsFromTmDbAsync(movieId);
            public virtual new Task<(int, string, List<string>, string)> GetMovieDetailsFromTmdbAsync(string title, DateTime releaseDate) => base.GetMovieDetailsFromTmdbAsync(title, releaseDate);
            public virtual new Task<Movie> BuildNewMovieAsync(string title, string cleanTitle, DateTime releaseDate, string fullLink, string id) => base.BuildNewMovieAsync(title, cleanTitle, releaseDate, fullLink, id);
            public virtual new Task UpdateExistingMovieAsync(Movie movie, string cleanTitle, DateTime releaseDate, string fullLink) => base.UpdateExistingMovieAsync(movie, cleanTitle, releaseDate, fullLink);
        }

        private readonly Mock<ILogger<ScraperService>> _loggerMock = new();
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
        private readonly Mock<IConfiguration> _configurationMock = new();

        string FirstCharToLower(string str) => $"{char.ToLower(str[0])}{str.Substring(1)}";

        static ScraperServiceTests()
        {
            ConfigureServer(new TestServerOptions
            {
                Licensing = new ServerOptions.LicensingOptions
                {
                    ThrowOnInvalidOrMissingLicense = false
                }
            });
        }

        protected override void PreInitialize(IDocumentStore documentStore)
        {
            documentStore.Conventions.Serialization = new NewtonsoftJsonSerializationConventions
            {
                CustomizeJsonSerializer = s => s.ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            documentStore.Conventions.PropertyNameConverter = memberInfo => FirstCharToLower(memberInfo.Name);
            documentStore.Conventions.FindCollectionName = type =>
            {
                if (type == typeof(Movie))
                    return "movies";
                return DocumentConventions.DefaultGetCollectionName(type);
            };
            documentStore.Conventions.MaxNumberOfRequestsPerSession = int.MaxValue;
            documentStore.Conventions.RegisterAsyncIdConvention<Movie>((dbname, metadata) => Task.FromResult($"movie/{metadata.Id}"));
            base.PreInitialize(documentStore);
        }

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
        public async Task ScrapeAsync_ReturnsEmptyList_WhenApiKeyMissing()
        {
            using var store = GetDocumentStore();
            var service = CreateService(store, apiKey: null);

            // Act
            var result = await service.ScrapeAsync(new int[0]);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ScrapeAsync_LogsWarning_WhenApiKeyMissing()
        {
            using var store = GetDocumentStore();
            var service = CreateService(store, apiKey: null);

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
            var service = CreateService(store, httpClient: httpClient);
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
            using var store = GetDocumentStore();
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
            var service1 = CreateService(store, httpClient: httpClient1);
            var result1 = await service1.ScrapeAsync(new[] { year });
            Assert.Single(result1);
            // Second scrape: movie is missing, should be deleted
            var handlerQueue2 = new Queue<HttpResponseMessage>(new[] { fakeTmdbResponse, fakeHtmlResponse2 });
            var httpClient2 = new HttpClient(new DelegatingHandlerStub(handlerQueue2));
            var service2 = CreateService(store, httpClient: httpClient2);
            var result2 = await service2.ScrapeAsync(new[] { year });
            Assert.Empty(result2);
        }

        [Fact]
        public async Task ScrapeAsync_DoesNotAddDuplicateMovies()
        {
            using var store = GetDocumentStore();
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
            var service = CreateService(store, httpClient: httpClient);
            var result = await service.ScrapeAsync(new[] { 2024 });
            Assert.Single(result);
        }

        [Fact]
        public async Task BuildNewMovieAsync_SetsScrapedAtToRecentValue()
        {
            var service = CreateTestableService(GetDocumentStore(), apiKey: null);
            var before = DateTimeOffset.UtcNow.AddSeconds(-5);
            var result = await service.BuildNewMovieAsync("Test", "Test", new DateTime(2024, 6, 1), "https://example.com", "test_2024-06-01");
            var after = DateTimeOffset.UtcNow.AddSeconds(5);
            Assert.True(result.ScrapedAt >= before && result.ScrapedAt <= after);
        }

        [Fact]
        public async Task ScrapeAsync_LogsInformation_WhenMovieAddedOrDeleted()
        {
            using var store = GetDocumentStore();
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
            var service = CreateService(store, httpClient: httpClient);
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
            using var store = GetDocumentStore();
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
            var service = CreateService(store, httpClient: httpClient);

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
