import { useState, useCallback } from 'react';
import Header from './components/Layout/Header';
import Footer from './components/Layout/Footer';
import CalendarView from './components/Calendar/CalendarView';
import SearchBar from './components/Search/SearchBar';
import SearchResults from './components/Search/SearchResults';
import MovieDetailModal from './components/MovieDetail/MovieDetailModal';
import SettingsPanel from './components/Settings/SettingsPanel';
import SeedingOverlay from './components/Seeding/SeedingOverlay';
import { usePreferences } from './hooks/usePreferences';
import { useTheme } from './hooks/useTheme';
import { useSearch } from './hooks/useSearch';
import type { CalendarViewType, MovieCalendarEvent, MovieSearchResult, TrailerLink } from './types';

type SelectedMovie = {
  title: string;
  date?: string;
  releaseDate?: string;
  description: string;
  url: string;
  posterUrl: string;
  mpaaRating: string;
  imdbId: string;
  genres: string[];
  directors: string[];
  cast: string[];
  trailers?: TrailerLink[];
};

export default function App() {
  const { prefs } = usePreferences();
  useTheme();

  const [currentView, setCurrentView] = useState<CalendarViewType>(
    (prefs?.defaultView as CalendarViewType) ?? 'dayGridMonth'
  );
  const [searchOpen, setSearchOpen] = useState(false);
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [selectedMovie, setSelectedMovie] = useState<SelectedMovie | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);
  const [calendarKey, setCalendarKey] = useState(0);

  const { results, loading: searchLoading, criteria, updateCriteria, reset: resetSearch } = useSearch();

  const handleSearchToggle = useCallback(() => {
    setSearchOpen((prev) => {
      if (prev) resetSearch();
      return !prev;
    });
  }, [resetSearch]);

  const handleMovieClick = useCallback((movie: MovieCalendarEvent) => {
    setSelectedMovie({
      title: movie.title,
      date: movie.date,
      description: movie.description,
      url: movie.url,
      posterUrl: movie.posterUrl,
      mpaaRating: movie.mpaaRating,
      imdbId: movie.imdbId,
      genres: movie.genres,
      directors: movie.directors,
      cast: movie.cast,
      trailers: movie.trailers,
    });
    setDetailOpen(true);
  }, []);

  const handleSearchResultClick = useCallback((result: MovieSearchResult) => {
    setSelectedMovie({
      title: result.title,
      releaseDate: result.releaseDate,
      description: result.description,
      url: result.url,
      posterUrl: result.posterUrl,
      mpaaRating: result.mpaaRating,
      imdbId: result.imdbId,
      genres: result.genres,
      directors: result.directors,
      cast: result.cast,
      trailers: result.trailers,
    });
    setDetailOpen(true);
  }, []);

  const handleSeeded = useCallback(() => {
    // Force calendar to refetch
    setCalendarKey((k) => k + 1);
  }, []);

  return (
    <div className="min-h-screen flex flex-col bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 transition-colors">
      <Header
        currentView={currentView}
        onViewChange={setCurrentView}
        onSearchToggle={handleSearchToggle}
        onSettingsToggle={() => setSettingsOpen(true)}
      />

      <main className="flex-1 relative">
        {searchOpen && (
          <div className="absolute inset-0 z-30 bg-white dark:bg-gray-900 overflow-auto">
            <div className="max-w-6xl mx-auto px-4 py-4">
              <SearchBar
                criteria={criteria}
                onUpdate={updateCriteria}
                onClose={() => {
                  setSearchOpen(false);
                  updateCriteria({ q: '' });
                }}
              />
              <SearchResults
                results={results}
                loading={searchLoading}
                onMovieClick={handleSearchResultClick}
              />
            </div>
          </div>
        )}

        <CalendarView
          key={calendarKey}
          view={currentView}
          onMovieClick={handleMovieClick}
        />
      </main>

      <Footer />

      <MovieDetailModal
        movie={selectedMovie}
        open={detailOpen}
        onClose={() => { setDetailOpen(false); setSelectedMovie(null); }}
      />

      <SettingsPanel open={settingsOpen} onClose={() => setSettingsOpen(false)} />

      <SeedingOverlay onSeeded={handleSeeded} />
    </div>
  );
}
