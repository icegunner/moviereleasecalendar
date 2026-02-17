import Modal from '../common/Modal';
import Badge from '../common/Badge';
import ExternalLinkIcon from '../common/ExternalLinkIcon';
import { formatDate } from '../../utils/dateUtils';
import type { TrailerLink } from '../../types';

/* Brand-colored site icons */
const siteIcons: Record<string, JSX.Element> = {
  YouTube: (
    <svg className="h-4 w-4 flex-shrink-0 text-red-600" viewBox="0 0 24 24" fill="currentColor">
      <path d="M23.5 6.19a3.02 3.02 0 0 0-2.12-2.14C19.5 3.5 12 3.5 12 3.5s-7.5 0-9.38.55A3.02 3.02 0 0 0 .5 6.19 31.6 31.6 0 0 0 0 12a31.6 31.6 0 0 0 .5 5.81 3.02 3.02 0 0 0 2.12 2.14c1.88.55 9.38.55 9.38.55s7.5 0 9.38-.55a3.02 3.02 0 0 0 2.12-2.14A31.6 31.6 0 0 0 24 12a31.6 31.6 0 0 0-.5-5.81zM9.75 15.02V8.98L15.5 12l-5.75 3.02z" />
    </svg>
  ),
  Vimeo: (
    <svg className="h-4 w-4 flex-shrink-0 text-[#1ab7ea]" viewBox="0 0 24 24" fill="currentColor">
      <path d="M23.98 6.22c-.1 2.26-1.68 5.35-4.74 9.28C16.07 19.5 13.27 21.5 10.9 21.5c-1.47 0-2.7-1.36-3.71-4.07l-2.02-7.42C4.41 7.3 3.6 5.94 2.73 5.94c-.18 0-.83.39-1.93 1.17L0 5.97c1.21-1.07 2.41-2.13 3.58-3.2C5.13 1.43 6.3.73 7.08.66c1.82-.18 2.94 1.07 3.36 3.73.45 2.88.77 4.67.94 5.38.52 2.37 1.1 3.56 1.72 3.56.49 0 1.22-.77 2.19-2.31.97-1.54 1.49-2.72 1.56-3.53.14-1.34-.39-2.01-1.57-2.01-.56 0-1.14.13-1.73.38C14.7 2.21 17.21.75 20.24.55c2.24-.15 3.3 1.31 3.74 5.67z" />
    </svg>
  ),
};

const defaultLinkIcon = (
  <svg className="h-4 w-4 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
    <path strokeLinecap="round" strokeLinejoin="round" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101" />
    <path strokeLinecap="round" strokeLinejoin="round" d="M10.172 13.828a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.102 1.101" />
  </svg>
);

interface MovieDetailModalProps {
  movie: {
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
  } | null;
  open: boolean;
  onClose: () => void;
}

export default function MovieDetailModal({ movie, open, onClose }: MovieDetailModalProps) {
  if (!movie) return null;

  const displayTitle = movie.title.replace(/^🎬\s*/, '');
  const releaseDate = movie.date || movie.releaseDate || '';

  return (
    <Modal open={open} onClose={onClose} title={displayTitle} wide>
      <div className="space-y-5">
        {/* ── Top section: Poster + Details side-by-side ── */}
        <div className="flex flex-col sm:flex-row gap-4">
          {/* Poster */}
          {movie.posterUrl && (
            <img
              src={movie.posterUrl}
              alt={displayTitle}
              className="w-40 h-60 object-cover rounded-lg flex-shrink-0 mx-auto sm:mx-0"
            />
          )}

          {/* Details */}
          <div className="flex-1 min-w-0 space-y-3">
            <div className="flex items-center gap-2 flex-wrap">
              {movie.mpaaRating && <Badge rating={movie.mpaaRating} />}
              <ExternalLinkIcon url={movie.url} imdbId={movie.imdbId} />
            </div>

            {releaseDate && (
              <p className="text-sm text-gray-500 dark:text-gray-400">
                <strong>Release Date:</strong> {formatDate(releaseDate)}
              </p>
            )}

            {movie.genres.length > 0 && (
              <div className="flex flex-wrap gap-1">
                {movie.genres.map((g) => (
                  <span
                    key={g}
                    className="text-xs bg-gray-200 dark:bg-gray-600 text-gray-600 dark:text-gray-300 rounded-full px-2 py-0.5"
                  >
                    {g}
                  </span>
                ))}
              </div>
            )}

            {movie.directors.length > 0 && (
              <p className="text-sm">
                <strong>Director{movie.directors.length > 1 ? 's' : ''}:</strong>{' '}
                {movie.directors.join(', ')}
              </p>
            )}

            {movie.cast.length > 0 && (
              <p className="text-sm">
                <strong>Cast:</strong> {movie.cast.join(', ')}
              </p>
            )}

            {movie.description && (
              <p className="text-sm text-gray-700 dark:text-gray-300 leading-relaxed whitespace-pre-line">
                {movie.description.split('\n').filter(l => !l.startsWith('http'))[0]}
              </p>
            )}
          </div>
        </div>

        {/* ── Bottom section: Trailers grid ── */}
        {movie.trailers && movie.trailers.length > 0 && (
          <div>
            <h3 className="text-sm font-semibold mb-2 border-t border-gray-200 dark:border-gray-700 pt-3">
              Trailers &amp; Teasers
            </h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2">
              {movie.trailers.map((t, i) => (
                <a
                  key={i}
                  href={t.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center gap-2 px-3 py-2 rounded-lg bg-gray-50 dark:bg-gray-800 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-sm"
                >
                  {siteIcons[t.site] ?? defaultLinkIcon}
                  <span className="truncate">{t.name}</span>
                </a>
              ))}
            </div>
          </div>
        )}
      </div>
    </Modal>
  );
}
