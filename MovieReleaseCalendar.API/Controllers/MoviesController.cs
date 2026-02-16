using System;
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
    public class MoviesController : ControllerBase
    {
        private readonly IMovieRepository _movieRepository;
        private readonly ILogger<MoviesController> _logger;

        public MoviesController(IMovieRepository movieRepository, ILogger<MoviesController> logger)
        {
            _movieRepository = movieRepository;
            _logger = logger;
        }

        /// <summary>
        /// Search movies by various criteria.
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] SearchCriteria criteria)
        {
            try
            {
                var movies = await _movieRepository.SearchMoviesAsync(criteria ?? new SearchCriteria());
                var results = movies.Select(m => new MovieSearchResult
                {
                    Id = m.Id,
                    Title = m.Title,
                    ReleaseDate = m.ReleaseDate,
                    Url = m.Url,
                    PosterUrl = m.PosterUrl,
                    Genres = m.Genres,
                    MpaaRating = m.MpaaRating,
                    ImdbId = m.ImdbId,
                    Directors = m.Directors,
                    Cast = m.Cast,
                    Description = m.Description
                }).ToList();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching movies.");
                return StatusCode(500, "An error occurred while searching movies.");
            }
        }

        /// <summary>
        /// Get a single movie by its ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var movie = await _movieRepository.GetMovieByIdAsync(id);
                if (movie == null)
                {
                    return NotFound();
                }

                return Ok(new MovieSearchResult
                {
                    Id = movie.Id,
                    Title = movie.Title,
                    ReleaseDate = movie.ReleaseDate,
                    Url = movie.Url,
                    PosterUrl = movie.PosterUrl,
                    Genres = movie.Genres,
                    MpaaRating = movie.MpaaRating,
                    ImdbId = movie.ImdbId,
                    Directors = movie.Directors,
                    Cast = movie.Cast,
                    Description = movie.Description
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching movie by ID: {Id}", id);
                return StatusCode(500, "An error occurred while fetching the movie.");
            }
        }
    }
}
