using MovieReleaseCalendar.API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
	public interface IScraperService
    {
        Task<List<Movie>> ScrapeAsync();
        Task<List<Movie>> ScrapeAsync(IEnumerable<int> years);
    }
}
