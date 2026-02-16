using System.Collections.Generic;
using Newtonsoft.Json;

namespace MovieReleaseCalendar.API.Models
{
    /// <summary>
    /// Deserialization model for TMDb GET /movie/{id}/release_dates endpoint.
    /// Used to extract the US theatrical MPAA certification.
    /// </summary>
    public class TmdbReleaseDatesResponse
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("results")]
        public List<TmdbReleaseDateCountry> Results { get; set; } = new List<TmdbReleaseDateCountry>();
    }

    public class TmdbReleaseDateCountry
    {
        [JsonProperty("iso_3166_1")]
        public string Iso3166_1 { get; set; } = string.Empty;

        [JsonProperty("release_dates")]
        public List<TmdbReleaseDate> ReleaseDates { get; set; } = new List<TmdbReleaseDate>();
    }

    public class TmdbReleaseDate
    {
        [JsonProperty("certification")]
        public string Certification { get; set; } = string.Empty;

        /// <summary>
        /// Release type: 1=Premiere, 2=Theatrical (limited), 3=Theatrical, 4=Digital, 5=Physical, 6=TV
        /// </summary>
        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("release_date")]
        public string ReleaseDate { get; set; } = string.Empty;
    }
}
