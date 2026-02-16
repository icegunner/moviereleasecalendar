using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Models;
using MovieReleaseCalendar.API.Services;

namespace MovieReleaseCalendar.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public partial class ScraperController : ControllerBase
    {
        private readonly IScraperService _scraperService;
        private readonly ILogger<ScraperController> _logger;

        public ScraperController(IScraperService scraperService, ILogger<ScraperController> logger)
        {
            _scraperService = scraperService;
            _logger = logger;
        }

        [HttpPost("run")]
        public async Task<ActionResult<List<Movie>>> RunScraper([FromBody] ScraperRunRequest request)
        {
            try
            {
                var years = (request?.Years != null && request.Years.Any())
                    ? request.Years.ToArray()
                    : [DateTime.UtcNow.Year - 1, DateTime.UtcNow.Year, DateTime.UtcNow.Year + 1];
                var result = await _scraperService.ScrapeAsync(years);
                if (result.HasError)
                {
                    return StatusCode(503, new { Error = result.Error });
                }
                return Ok(new { Imported = result.NewMovies.Count, Updated = result.UpdatedMovies.Count, NewMovies = result.NewMovies, UpdatedMovies = result.UpdatedMovies });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while running scraper.");
                return StatusCode(500, "An error occurred while scraping.");
            }
        }
    }
}
