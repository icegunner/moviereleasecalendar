using System;

namespace MovieCalendar.API.Models
{
    public class MovieCalendarEvent
    {
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string Emoji { get; set; }
        public bool AllDay { get; set; } = true;
    }
}
