using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Services;
using MovieReleaseCalendar.API.Models;

namespace MovieReleaseCalendar.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PreferencesController : ControllerBase
    {
        private readonly IPreferencesRepository _preferencesRepository;
        private readonly ILogger<PreferencesController> _logger;

        public PreferencesController(IPreferencesRepository preferencesRepository, ILogger<PreferencesController> logger)
        {
            _preferencesRepository = preferencesRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get the current global preferences.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPreferences()
        {
            try
            {
                var prefs = await _preferencesRepository.GetPreferencesAsync();
                return Ok(prefs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching preferences.");
                return StatusCode(500, "An error occurred while fetching preferences.");
            }
        }

        /// <summary>
        /// Update global preferences.
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdatePreferences([FromBody] UserPreferences preferences)
        {
            try
            {
                if (preferences == null)
                {
                    return BadRequest("Preferences body is required.");
                }

                // Ensure the ID is always "global"
                preferences.Id = "global";
                preferences.UpdatedAt = DateTimeOffset.UtcNow;

                await _preferencesRepository.SavePreferencesAsync(preferences);
                return Ok(preferences);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving preferences.");
                return StatusCode(500, "An error occurred while saving preferences.");
            }
        }
    }
}
