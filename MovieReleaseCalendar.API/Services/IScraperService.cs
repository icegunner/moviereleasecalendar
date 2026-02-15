using MovieReleaseCalendar.API.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
	public interface IScraperService
    {
        Task<List<Movie>> ScrapeAsync(CancellationToken cancellationToken = default);
        Task<List<Movie>> ScrapeAsync(IEnumerable<int> years, CancellationToken cancellationToken = default);
    }
}
