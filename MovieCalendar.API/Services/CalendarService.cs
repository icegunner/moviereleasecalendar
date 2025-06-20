using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.Extensions.Logging;
using MovieCalendar.API.Models;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieCalendar.API.Services
{
    public class CalendarService
    {
        private readonly IDocumentStore _store;
        private readonly ILogger<CalendarService> _logger;

        public CalendarService(IDocumentStore store, ILogger<CalendarService> logger)
        {
            _store = store;
            _logger = logger;
        }

        public async Task<List<MovieCalendarEvent>> GetCalendarEventsAsync()
        {
            using var session = _store.OpenAsyncSession();

            var movies = await session
                .Query<Movie>()
                .Where(m => m.ReleaseDate >= DateTime.Today.AddYears(-1) && m.ReleaseDate <= DateTime.Today.AddYears(2))
                .OrderBy(m => m.ReleaseDate)
                .ToListAsync();

            return movies.Select(movie =>
				new MovieCalendarEvent
				{
					Title = $"🎬 {movie.Title}",
					Date = movie.ReleaseDate.Date,
					Url = movie.Url,
					Description = movie.Description,
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
