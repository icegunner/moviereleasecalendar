using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
    public class CalendarService : ICalendarService
    {
        private readonly IMovieRepository _movieRepository;
        private readonly ILogger<CalendarService> _logger;

        public CalendarService(IMovieRepository movieRepository, ILogger<CalendarService> logger)
        {
            _movieRepository = movieRepository;
            _logger = logger;
        }

        public async Task<List<MovieCalendarEvent>> GetCalendarEventsAsync()
        {
            return await GetCalendarEventsAsync(null, null);
        }

        public async Task<List<MovieCalendarEvent>> GetCalendarEventsAsync(DateTime? start, DateTime? end)
        {
            var queryStart = start ?? DateTime.Today.AddYears(-1);
            var queryEnd = end ?? DateTime.Today.AddYears(2);

            var movies = await _movieRepository.GetMoviesInRangeAsync(queryStart, queryEnd);

            return movies.Select(movie =>
                new MovieCalendarEvent
                {
                    Title = $"🎬 {movie.Title}",
                    Date = movie.ReleaseDate.Date,
                    Url = movie.Url,
                    Description = movie.Description,
                    PosterUrl = movie.PosterUrl,
                    AllDay = true
                }
            )
            .ToList();
        }

        public async Task<string> GenerateIcsFeedAsync()
        {
            var calendar = new Ical.Net.Calendar();
            var events = await GetCalendarEventsAsync();

            foreach (var ev in events)
            {
                var icalEvent = new CalendarEvent
                {
                    Summary = ev.Title,
                    DtStart = new CalDateTime(ev.Date.Date),
                    DtEnd = new CalDateTime(ev.Date.Date.AddDays(1)),
                    Description = ev.Description,
                    Url = string.IsNullOrWhiteSpace(ev.Url) ? null : new Uri(ev.Url)
                };

                calendar.Events.Add(icalEvent);
            }

            var serializer = new CalendarSerializer(new SerializationContext());
            return serializer.SerializeToString(calendar) ?? string.Empty;
        }
    }
}
