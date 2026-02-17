import type { MouseEvent } from 'react';
import type { MovieCalendarEvent } from '../../types';
import Badge from '../common/Badge';
import ExternalLinkIcon from '../common/ExternalLinkIcon';

interface MovieEventProps {
  movie: MovieCalendarEvent;
  showRatings: boolean;
  onMouseEnter: (e: MouseEvent) => void;
  onMouseLeave: () => void;
  onClick: () => void;
}

export default function MovieEvent({
  movie,
  showRatings,
  onMouseEnter,
  onMouseLeave,
  onClick,
}: MovieEventProps) {
  // Strip the 🎬 prefix for cleaner display
  const displayTitle = movie.title.replace(/^🎬\s*/, '');

  return (
    <div
      className="flex items-center gap-1 px-1 py-0.5 cursor-pointer text-xs leading-tight truncate w-full"
      onMouseEnter={onMouseEnter}
      onMouseLeave={onMouseLeave}
      onClick={onClick}
    >
      <span className="truncate font-medium">{displayTitle}</span>
      {showRatings && movie.mpaaRating && (
        <Badge rating={movie.mpaaRating} className="flex-shrink-0" />
      )}
      <ExternalLinkIcon url={movie.url} imdbId={movie.imdbId} />
    </div>
  );
}
