using Microsoft.AspNetCore.Mvc;

namespace MovieReleaseCalendar.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        /// <summary>
        /// Static flag set by StartupSeeder to indicate initial seeding is in progress.
        /// </summary>
        public static bool IsSeeding { get; set; }

        /// <summary>
        /// Returns the current application status, including whether initial seeding is in progress.
        /// </summary>
        [HttpGet]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                Status = IsSeeding ? "seeding" : "ready",
                IsSeeding
            });
        }
    }
}
