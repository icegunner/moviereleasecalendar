using MovieReleaseCalendar.API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
	public interface ICalendarService
    {
        Task<List<MovieCalendarEvent>> GetCalendarEventsAsync();
        Task<string> GenerateIcsFeedAsync();
    }
}
