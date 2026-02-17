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
        [JsonProperty("tmdbId")]
        public int TmdbId { get; set; }
        [JsonProperty("imdbId")]
        public string ImdbId { get; set; } = string.Empty;
        [JsonProperty("directors")]
        public List<string> Directors { get; set; } = new List<string>();
        [JsonProperty("cast")]
        public List<string> Cast { get; set; } = new List<string>();
        [JsonProperty("mpaaRating")]
        public string MpaaRating { get; set; } = string.Empty;
        [JsonProperty("trailers")]
        public List<TrailerLink> Trailers { get; set; } = new List<TrailerLink>();
    }

    public class TrailerLink
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        [JsonProperty("url")]
        public string Url { get; set; } = string.Empty;
        [JsonProperty("site")]
        public string Site { get; set; } = string.Empty;
        [JsonProperty("publishedAt")]
        public DateTimeOffset? PublishedAt { get; set; }
    }
}
