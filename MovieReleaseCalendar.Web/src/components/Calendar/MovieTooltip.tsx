import { useRef, useLayoutEffect } from 'react';
import type { MovieCalendarEvent } from '../../types';

interface MovieTooltipProps {
  movie: MovieCalendarEvent;
  x: number;
  y: number;
}

export default function MovieTooltip({ movie, x, y }: MovieTooltipProps) {
  const displayTitle = movie.title.replace(/^🎬\s*/, '');
  const ref = useRef<HTMLDivElement>(null);

  // Position after render so we can measure actual size
  useLayoutEffect(() => {
    const el = ref.current;
    if (!el) return;

    const rect = el.getBoundingClientRect();
    const gap = 8;
    let left = x + gap;
    let top = y + gap;

    // Flip left if bleeding right
    if (left + rect.width > window.innerWidth - 4) {
      left = x - rect.width - gap;
    }
    // Flip above if bleeding bottom — use actual measured height
    if (top + rect.height > window.innerHeight - 4) {
      top = y - rect.height - gap;
    }
    // Clamp
    if (left < 4) left = 4;
    if (top < 4) top = 4;

    el.style.left = `${left}px`;
    el.style.top = `${top}px`;
    el.style.visibility = 'visible';
  }, [x, y]);

  return (
    <div
      ref={ref}
      className="fixed z-50 bg-white dark:bg-gray-800 rounded-lg shadow-xl border border-gray-200 dark:border-gray-700 p-3 max-w-xs pointer-events-none"
      style={{ visibility: 'hidden', left: 0, top: 0 }}
    >
      <div className="flex gap-3">
        {movie.posterUrl && (
          <img
            src={movie.posterUrl}
            alt={displayTitle}
            className="w-16 h-24 object-cover rounded flex-shrink-0"
          />
        )}
        <div className="min-w-0">
          <h4 className="font-semibold text-sm truncate">{displayTitle}</h4>
          {movie.mpaaRating && (
            <span className="text-xs text-gray-500 dark:text-gray-400">
              Rated {movie.mpaaRating}
            </span>
          )}
          {movie.genres.length > 0 && (
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
              {movie.genres.join(', ')}
            </p>
          )}
          {movie.directors.length > 0 && (
            <p className="text-xs text-gray-500 dark:text-gray-400">
              Dir: {movie.directors.join(', ')}
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
