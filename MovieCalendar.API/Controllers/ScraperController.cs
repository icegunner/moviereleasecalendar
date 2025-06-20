using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MovieCalendar.API.Models;
using MovieCalendar.API.Services;

namespace MovieCalendar.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScraperController : ControllerBase
    {
        private readonly ScraperService _scraperService;
        private readonly ILogger<ScraperController> _logger;

        public ScraperController(ScraperService scraperService, ILogger<ScraperController> logger)
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
