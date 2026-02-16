namespace MovieReleaseCalendar.API.Models
{
    public class SearchCriteria
    {
        /// <summary>Partial title match</summary>
        public string Q { get; set; }

        /// <summary>Genre name (exact or partial)</summary>
        public string Genre { get; set; }

        /// <summary>Director name (partial)</summary>
        public string Director { get; set; }

        /// <summary>Cast member name (partial)</summary>
        public string Cast { get; set; }

        /// <summary>MPAA rating (G, PG, PG-13, R, NC-17)</summary>
        public string Rating { get; set; }

        /// <summary>Release year</summary>
        public int? Year { get; set; }

        /// <summary>Release month (1-12)</summary>
        public int? Month { get; set; }

        /// <summary>Exact IMDB ID</summary>
        public string ImdbId { get; set; }
    }
}
