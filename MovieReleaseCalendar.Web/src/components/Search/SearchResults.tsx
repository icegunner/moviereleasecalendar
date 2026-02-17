import type { MovieSearchResult } from '../../types';
import SearchResultCard from './SearchResultCard';
import Spinner from '../common/Spinner';

interface SearchResultsProps {
  results: MovieSearchResult[];
  loading: boolean;
  onMovieClick: (movie: MovieSearchResult) => void;
}

export default function SearchResults({ results, loading, onMovieClick }: SearchResultsProps) {
  if (loading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner size="lg" />
      </div>
    );
  }

  if (results.length === 0) {
    return (
      <div className="text-center py-12 text-gray-500 dark:text-gray-400">
        No results found. Try adjusting your search criteria.
      </div>
    );
  }

  return (
    <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 p-4 max-w-7xl mx-auto">
      {results.map((movie) => (
        <SearchResultCard key={movie.id} movie={movie} onClick={() => onMovieClick(movie)} />
      ))}
    </div>
  );
}
