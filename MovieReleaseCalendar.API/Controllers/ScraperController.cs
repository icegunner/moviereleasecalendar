using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MovieReleaseCalendar.API.Models;
using MovieReleaseCalendar.API.Services;

namespace MovieReleaseCalendar.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScraperController : ControllerBase
    {
        private readonly IScraperService _scraperService;
        private readonly ILogger<ScraperController> _logger;

        public ScraperController(IScraperService scraperService, ILogger<ScraperController> logger)
        {
            _scraperService = scraperService;
            _logger = logger;
        }

        [HttpPost("run")]
        public async Task<ActionResult<List<Movie>>> RunScraper()
        {
            try
            {
                var result = await _scraperService.ScrapeAsync();
                return Ok(new { Imported = result.Count, Movies = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while running scraper.");
                return StatusCode(500, "An error occurred while scraping.");
            }
        }
    }
}
