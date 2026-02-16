using MovieReleaseCalendar.API.Models;
using System.Threading.Tasks;

namespace MovieReleaseCalendar.API.Services
{
    public interface IPreferencesRepository
    {
        Task<UserPreferences> GetPreferencesAsync();
        Task SavePreferencesAsync(UserPreferences prefs);
    }
}
