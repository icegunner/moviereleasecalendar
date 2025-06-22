using System.Collections.Generic;
using Newtonsoft.Json;

namespace MovieCalendar.API.Models
{
    public class TmDbGenreResponse
    {
        [JsonProperty("genres")]
        public List<TmDbGenre> Genres { get; set; } = new List<TmDbGenre>();
    }

    public class TmDbGenre
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

}