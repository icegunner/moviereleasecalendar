import { useMemo } from 'react';
import type { MovieCalendarEvent } from '../../types';
import Badge from '../common/Badge';
import ExternalLinkIcon from '../common/ExternalLinkIcon';
import { formatDate } from '../../utils/dateUtils';

interface WeekViewProps {
  events: MovieCalendarEvent[];
  showRatings: boolean;
  onMovieClick: (movie: MovieCalendarEvent) => void;
}

const dayNames = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

/** Get the Sunday that starts the current week */
function getWeekStart(date: Date): Date {
  const d = new Date(date);
  d.setHours(0, 0, 0, 0);
  d.setDate(d.getDate() - d.getDay()); // Roll back to Sunday
  return d;
}

interface DayGroup {
  dayName: string;
  date: Date;
  label: string;
  movies: MovieCalendarEvent[];
}

export default function WeekView({ events, showRatings, onMovieClick }: WeekViewProps) {
  const groups = useMemo(() => {
    const weekStart = getWeekStart(new Date());
    const days: DayGroup[] = [];

    for (let i = 0; i < 7; i++) {
      const d = new Date(weekStart);
      d.setDate(d.getDate() + i);
      days.push({
        dayName: dayNames[i],
        date: d,
        label: `${dayNames[i]}, ${formatDate(d.toISOString().slice(0, 10))}`,
        movies: [],
      });
    }

    const weekEnd = new Date(weekStart);
    weekEnd.setDate(weekEnd.getDate() + 7);

    for (const ev of events) {
      const evDate = new Date(ev.date);
      if (evDate >= weekStart && evDate < weekEnd) {
        const dayIdx = evDate.getDay();
        days[dayIdx].movies.push(ev);
      }
    }

    // Sort movies within each day
    for (const day of days) {
      day.movies.sort((a, b) => a.title.localeCompare(b.title));
    }

    return days;
  }, [events]);

  const hasAny = groups.some((g) => g.movies.length > 0);

  if (!hasAny) {
    return (
      <div className="text-center py-12 text-gray-500 dark:text-gray-400">
        No movies releasing this week.
      </div>
    );
  }

  return (
    <div className="space-y-6 py-4">
      {groups.map((group) => (
        <section key={group.dayName}>
          <h2
            className={`text-lg font-bold mb-2 px-2 py-2 sticky top-16 backdrop-blur border-b border-gray-200 dark:border-gray-700 ${
              group.movies.length > 0
                ? 'text-gray-800 dark:text-gray-200 bg-white/95 dark:bg-gray-900/95'
                : 'text-gray-400 dark:text-gray-600 bg-white/90 dark:bg-gray-900/90'
            }`}
          >
            {group.label}
            {group.movies.length > 0 && (
              <span className="ml-2 text-sm font-normal text-gray-500 dark:text-gray-400">
                ({group.movies.length} movie{group.movies.length !== 1 ? 's' : ''})
              </span>
            )}
          </h2>
          {group.movies.length === 0 ? (
            <p className="px-4 text-sm text-gray-400 dark:text-gray-600 italic">
              No releases
            </p>
          ) : (
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
          )}
        </section>
      ))}
    </div>
  );
}
