using MovieReleaseCalendar.API.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
	public interface ICalendarService
    {
        Task<List<MovieCalendarEvent>> GetCalendarEventsAsync();
        Task<List<MovieCalendarEvent>> GetCalendarEventsAsync(DateTime? start, DateTime? end);
        Task<string> GenerateIcsFeedAsync();
    }
}
