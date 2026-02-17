import type { MovieSearchResult } from '../../types';
import Badge from '../common/Badge';
import ExternalLinkIcon from '../common/ExternalLinkIcon';
import { formatDate } from '../../utils/dateUtils';

interface SearchResultCardProps {
  movie: MovieSearchResult;
  onClick: () => void;
}

export default function SearchResultCard({ movie, onClick }: SearchResultCardProps) {
  return (
    <div
      className="flex gap-3 p-3 rounded-lg bg-gray-50 dark:bg-gray-800 hover:bg-gray-100 dark:hover:bg-gray-700 cursor-pointer transition-colors border border-gray-200 dark:border-gray-700"
      onClick={onClick}
    >
      {movie.posterUrl ? (
        <img
          src={movie.posterUrl}
          alt={movie.title}
          className="w-16 h-24 object-cover rounded flex-shrink-0"
          loading="lazy"
        />
      ) : (
        <div className="w-16 h-24 bg-gray-200 dark:bg-gray-700 rounded flex-shrink-0 flex items-center justify-center text-gray-400 text-xs">
          N/A
        </div>
      )}
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-1 flex-wrap">
          <h3 className="font-semibold text-sm">{movie.title}</h3>
          {movie.mpaaRating && <Badge rating={movie.mpaaRating} />}
          <ExternalLinkIcon url={movie.url} imdbId={movie.imdbId} />
        </div>
        <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
          {formatDate(movie.releaseDate)}
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
        {movie.directors.length > 0 && (
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
            Dir: {movie.directors.join(', ')}
          </p>
        )}
      </div>
    </div>
  );
}
