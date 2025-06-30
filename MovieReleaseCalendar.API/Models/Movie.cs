using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MovieReleaseCalendar.API.Models
{
    public class Movie
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        [JsonPropertyName("releaseDate")]
        public DateTime ReleaseDate { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new List<string>();
        [JsonPropertyName("posterUrl")]
        public string PosterUrl { get; set; } = string.Empty;
        [JsonPropertyName("scrapedAt")]
        public DateTimeOffset ScrapedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
