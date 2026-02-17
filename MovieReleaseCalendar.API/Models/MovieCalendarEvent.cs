using System;
using System.Collections.Generic;

namespace MovieReleaseCalendar.API.Models
{
    public class MovieCalendarEvent
    {
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string PosterUrl { get; set; }
        public bool AllDay { get; set; } = true;
        public int TmdbId { get; set; }
        public string ImdbId { get; set; } = string.Empty;
        public string MpaaRating { get; set; } = string.Empty;
        public List<string> Genres { get; set; } = new List<string>();
        public List<string> Directors { get; set; } = new List<string>();
        public List<string> Cast { get; set; } = new List<string>();
        public List<TrailerLink> Trailers { get; set; } = new List<TrailerLink>();
    }
}
