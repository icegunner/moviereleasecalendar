using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MovieReleaseCalendar.API.Models
{
    public class Movie
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;
        [JsonProperty("releaseDate")]
        public DateTime ReleaseDate { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; } = string.Empty;
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
        [JsonProperty("genres")]
        public List<string> Genres { get; set; } = new List<string>();
        [JsonProperty("posterUrl")]
        public string PosterUrl { get; set; } = string.Empty;
        [JsonProperty("scrapedAt")]
        public DateTimeOffset ScrapedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
