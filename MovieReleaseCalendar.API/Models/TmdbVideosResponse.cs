using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MovieReleaseCalendar.API.Models
{
    public class TmdbVideosResponse
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("results")]
        public List<TmdbVideo> Results { get; set; } = new List<TmdbVideo>();
    }

    public class TmdbVideo
    {
        [JsonProperty("iso_639_1")]
        public string Iso639_1 { get; set; } = string.Empty;

        [JsonProperty("iso_3166_1")]
        public string Iso3166_1 { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("key")]
        public string Key { get; set; } = string.Empty;

        [JsonProperty("site")]
        public string Site { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("official")]
        public bool Official { get; set; }

        [JsonProperty("published_at")]
        public DateTimeOffset? PublishedAt { get; set; }
    }
}
