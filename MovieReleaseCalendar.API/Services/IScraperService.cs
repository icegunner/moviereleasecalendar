using MovieReleaseCalendar.API.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
	public interface IScraperService
    {
        Task<ScrapeResult> ScrapeAsync(CancellationToken cancellationToken = default);
        Task<ScrapeResult> ScrapeAsync(IEnumerable<int> years, CancellationToken cancellationToken = default);
    }
}
