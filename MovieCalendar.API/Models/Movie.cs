using System;
using System.Collections.Generic;

namespace MovieCalendar.API.Models
{
    public class Movie
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string>? Genres { get; set; }
        public string? PosterUrl { get; set; }
        public DateTimeOffset ScrapedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
