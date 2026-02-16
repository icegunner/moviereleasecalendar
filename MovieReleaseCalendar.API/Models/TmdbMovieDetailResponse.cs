using Newtonsoft.Json;

namespace MovieReleaseCalendar.API.Models
{
    /// <summary>
    /// Deserialization model for TMDb GET /movie/{id} endpoint.
    /// Used to extract the IMDB ID.
    /// </summary>
    public class TmdbMovieDetailResponse
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("imdb_id")]
        public string ImdbId { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;
    }
}
