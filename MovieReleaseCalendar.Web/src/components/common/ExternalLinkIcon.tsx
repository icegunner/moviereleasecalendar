import { isFirstShowingUrl } from '../../utils/dateUtils';

interface ExternalLinkIconProps {
  url?: string;
  imdbId?: string;
  showRatings?: boolean;
}

/**
 * Renders FS icon (left) and/or IMDB icon (right) as inline links.
 * FS icon only shows if URL contains firstshowing.net.
 */
export default function ExternalLinkIcon({ url, imdbId }: ExternalLinkIconProps) {
  const showFs = isFirstShowingUrl(url);
  const showImdb = !!imdbId;

  if (!showFs && !showImdb) return null;

  return (
    <span className="inline-flex items-center gap-1 ml-1">
      {showFs && (
        <a
          href={url}
          target="_blank"
          rel="noopener noreferrer"
          title="View on firstshowing.net"
          className="inline-flex items-center hover:opacity-80"
          onClick={(e) => e.stopPropagation()}
        >
          <span className="text-xs font-bold text-red-600 dark:text-red-500 border border-red-600 dark:border-red-500 rounded px-0.5 leading-tight">
            FS
          </span>
        </a>
      )}
      {showImdb && (
        <a
          href={`https://www.imdb.com/title/${imdbId}/`}
          target="_blank"
          rel="noopener noreferrer"
          title="View on IMDB"
          className="inline-flex items-center hover:opacity-80"
          onClick={(e) => e.stopPropagation()}
        >
          <span className="text-xs font-bold text-yellow-600 dark:text-yellow-400 border border-yellow-600 dark:border-yellow-400 rounded px-0.5 leading-tight">
            IMDb
          </span>
        </a>
      )}
    </span>
  );
}
