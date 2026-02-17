using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
                    AllDay = true,
                    TmdbId = movie.TmdbId,
                    ImdbId = movie.ImdbId,
                    MpaaRating = movie.MpaaRating,
                    Genres = movie.Genres ?? new List<string>(),
                    Directors = movie.Directors ?? new List<string>(),
                    Cast = movie.Cast ?? new List<string>(),
                    Trailers = movie.Trailers ?? new List<TrailerLink>()
                }
            )
            .ToList();
        }

        public async Task<string> GenerateIcsFeedAsync()
        {
            var calendar = new Ical.Net.Calendar();
            calendar.ProductId = "-//MovieReleaseCalendar//EN";
            calendar.Method = "PUBLISH";
            calendar.AddProperty("X-WR-CALNAME", "Movie Release Calendar");
            calendar.AddProperty("X-WR-TIMEZONE", "America/New_York");
            calendar.AddProperty("X-WR-CALDESC", "A release calendar for major nationwide theatrical movie openings. Based on firstshowing.net and their old Google Calendar.");

            var events = await GetCalendarEventsAsync();

            foreach (var ev in events)
            {
                var dtStart = new CalDateTime(DateOnly.FromDateTime(ev.Date.Date));
                var dtEnd = new CalDateTime(DateOnly.FromDateTime(ev.Date.Date.AddDays(1)));

                var icalEvent = new CalendarEvent
                {
                    Summary = ev.Title,
                    DtStart = dtStart,
                    DtEnd = dtEnd,
                    Description = ev.Description,
                    Url = string.IsNullOrWhiteSpace(ev.Url) ? null : new Uri(ev.Url),
                    Transparency = TransparencyType.Transparent,
                    Status = EventStatus.Confirmed,
                    Uid = GenerateStableUid(ev.Title, ev.Date)
                };
                icalEvent.Properties.Add(new CalendarProperty("SEQUENCE", "0"));

                calendar.Events.Add(icalEvent);
            }

            var serializer = new CalendarSerializer(new SerializationContext());
            return serializer.SerializeToString(calendar) ?? string.Empty;
        }

        private static string GenerateStableUid(string title, DateTime date)
        {
            var input = $"{title}|{date:yyyy-MM-dd}";
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant()[..16];
            return $"{hash}@moviereleacecalendar";
        }
    }
}
