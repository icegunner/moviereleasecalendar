using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Services;

namespace MovieReleaseCalendar.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarService _calendarService;
        private readonly ILogger<CalendarController> _logger;

        public CalendarController(ICalendarService calendarService, ILogger<CalendarController> logger)
        {
            _calendarService = calendarService;
            _logger = logger;
        }

        [HttpGet("events.json")]
        public async Task<IActionResult> GetJsonEvents()
        {
            try
            {
                var events = await _calendarService.GetCalendarEventsAsync();
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch JSON calendar events.");
                return StatusCode(500, "Error fetching events.");
            }
        }

        [HttpGet("calendar.ics")]
        public async Task<IActionResult> GetIcsCalendar()
        {
            try
            {
                var icsContent = await _calendarService.GenerateIcsFeedAsync();
                return File(System.Text.Encoding.UTF8.GetBytes(icsContent), "text/calendar", "calendar.ics");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate ICS feed.");
                return StatusCode(500, "Error generating ICS.");
            }
        }
    }
}
