using System.Collections.Generic;

namespace MovieReleaseCalendar.API.Models
{
    public class ScrapeResult
    {
        public List<Movie> NewMovies { get; set; } = new();
        public List<Movie> UpdatedMovies { get; set; } = new();
    }
}
