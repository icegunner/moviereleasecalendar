using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MovieReleaseCalendar.API.Models
{
    /// <summary>
    /// DTO returned by the search API with all relevant display fields.
    /// </summary>
    public class MovieSearchResult
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("releaseDate")]
        public DateTime ReleaseDate { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; } = string.Empty;

        [JsonProperty("posterUrl")]
        public string PosterUrl { get; set; } = string.Empty;

        [JsonProperty("genres")]
        public List<string> Genres { get; set; } = new List<string>();

        [JsonProperty("mpaaRating")]
        public string MpaaRating { get; set; } = string.Empty;

        [JsonProperty("imdbId")]
        public string ImdbId { get; set; } = string.Empty;

        [JsonProperty("directors")]
        public List<string> Directors { get; set; } = new List<string>();

        [JsonProperty("cast")]
        public List<string> Cast { get; set; } = new List<string>();

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
    }
}
