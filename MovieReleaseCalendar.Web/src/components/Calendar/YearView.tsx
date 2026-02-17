import { useMemo } from 'react';
import type { MovieCalendarEvent } from '../../types';
import Badge from '../common/Badge';
import ExternalLinkIcon from '../common/ExternalLinkIcon';
import { monthName, formatDate } from '../../utils/dateUtils';

interface YearViewProps {
  events: MovieCalendarEvent[];
  showRatings: boolean;
  onMovieClick: (movie: MovieCalendarEvent) => void;
}

interface MonthGroup {
  month: number;
  year: number;
  label: string;
  movies: MovieCalendarEvent[];
}

export default function YearView({ events, showRatings, onMovieClick }: YearViewProps) {
  const grouped = useMemo(() => {
    const map = new Map<string, MonthGroup>();
    const sorted = [...events].sort(
      (a, b) => new Date(a.date).getTime() - new Date(b.date).getTime()
    );
    for (const ev of sorted) {
      const d = new Date(ev.date);
      const key = `${d.getFullYear()}-${d.getMonth()}`;
      if (!map.has(key)) {
        map.set(key, {
          month: d.getMonth() + 1,
          year: d.getFullYear(),
          label: `${monthName(d.getMonth() + 1)} ${d.getFullYear()}`,
          movies: [],
        });
      }
      map.get(key)!.movies.push(ev);
    }
    return Array.from(map.values());
  }, [events]);

  if (grouped.length === 0) {
    return (
      <div className="text-center py-12 text-gray-500 dark:text-gray-400">
        No movies to display.
      </div>
    );
  }

  return (
    <div className="space-y-8 py-4">
      {grouped.map((group) => (
        <section key={`${group.year}-${group.month}`}>
          <h2 className="text-xl font-bold mb-3 px-2 text-gray-800 dark:text-gray-200 sticky top-16 bg-white/95 dark:bg-gray-900/95 backdrop-blur py-2 border-b border-gray-200 dark:border-gray-700">
            {group.label}
          </h2>
          <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3 px-2">
            {group.movies.map((movie, i) => {
              const displayTitle = movie.title.replace(/^🎬\s*/, '');
              return (
                <div
                  key={`${movie.date}-${i}`}
                  className="flex gap-3 p-3 rounded-lg bg-gray-50 dark:bg-gray-800 hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer transition-colors border border-gray-200 dark:border-gray-700"
                  onClick={() => onMovieClick(movie)}
                >
                  {movie.posterUrl ? (
                    <img
                      src={movie.posterUrl}
                      alt={displayTitle}
                      className="w-12 h-18 object-cover rounded flex-shrink-0"
                      loading="lazy"
                    />
                  ) : (
                    <div className="w-12 h-18 bg-gray-200 dark:bg-gray-700 rounded flex-shrink-0 flex items-center justify-center text-gray-400 text-xs">
                      N/A
                    </div>
                  )}
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-1 flex-wrap">
                      <h3 className="font-semibold text-sm truncate">{displayTitle}</h3>
                      {showRatings && movie.mpaaRating && <Badge rating={movie.mpaaRating} />}
                      <ExternalLinkIcon url={movie.url} imdbId={movie.imdbId} />
                    </div>
                    <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                      {formatDate(movie.date)}
                    </p>
                    {movie.genres.length > 0 && (
                      <div className="flex flex-wrap gap-1 mt-1">
                        {movie.genres.map((g) => (
                          <span
                            key={g}
                            className="text-xs bg-gray-200 dark:bg-gray-600 text-gray-600 dark:text-gray-300 rounded px-1.5 py-0.5"
                          >
                            {g}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </section>
      ))}
    </div>
  );
}
