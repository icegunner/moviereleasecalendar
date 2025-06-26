using System.Collections.Generic;
using Newtonsoft.Json;

namespace MovieReleaseCalendar.API.Models
{
    public class TmDbGenreResponse
    {
        [JsonProperty("genres")]
        public List<TmDbGenre> Genres { get; set; } = new List<TmDbGenre>();
    }

    public class TmDbGenre
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
    }

}