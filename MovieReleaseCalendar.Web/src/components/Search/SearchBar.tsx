import { MagnifyingGlassIcon, XMarkIcon } from '@heroicons/react/24/outline';
import type { SearchCriteria } from '../../types';

interface SearchBarProps {
  criteria: SearchCriteria;
  onUpdate: (patch: Partial<SearchCriteria>) => void;
  onClose: () => void;
}

const ratingOptions = ['', 'G', 'PG', 'PG-13', 'R', 'NC-17'];
const currentYear = new Date().getFullYear();
const yearOptions = Array.from({ length: 5 }, (_, i) => currentYear - 1 + i);

export default function SearchBar({ criteria, onUpdate, onClose }: SearchBarProps) {
  return (
    <div className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 px-4 py-3">
      <div className="max-w-7xl mx-auto space-y-3">
        {/* Text search row */}
        <div className="flex items-center gap-2">
          <MagnifyingGlassIcon className="h-5 w-5 text-gray-400 flex-shrink-0" />
          <input
            type="text"
            placeholder="Search movies by title..."
            value={criteria.q || ''}
            onChange={(e) => onUpdate({ q: e.target.value })}
            className="flex-1 bg-transparent outline-none text-sm placeholder-gray-400"
            autoFocus
          />
          <button
            onClick={onClose}
            className="p-1 rounded hover:bg-gray-100 dark:hover:bg-gray-700"
            title="Close search"
          >
            <XMarkIcon className="h-5 w-5" />
          </button>
        </div>

        {/* Filter row */}
        <div className="flex flex-wrap items-center gap-2 text-sm">
          <select
            value={criteria.rating || ''}
            onChange={(e) => onUpdate({ rating: e.target.value || undefined })}
            className="bg-gray-100 dark:bg-gray-700 rounded px-2 py-1 text-sm border-0 outline-none"
          >
            <option value="">All Ratings</option>
            {ratingOptions.filter(Boolean).map((r) => (
              <option key={r} value={r}>
                {r}
              </option>
            ))}
          </select>

          <select
            value={criteria.year ?? ''}
            onChange={(e) =>
              onUpdate({ year: e.target.value ? Number(e.target.value) : undefined })
            }
            className="bg-gray-100 dark:bg-gray-700 rounded px-2 py-1 text-sm border-0 outline-none"
          >
            <option value="">All Years</option>
            {yearOptions.map((y) => (
              <option key={y} value={y}>
                {y}
              </option>
            ))}
          </select>

          <input
            type="text"
            placeholder="Genre"
            value={criteria.genre || ''}
            onChange={(e) => onUpdate({ genre: e.target.value || undefined })}
            className="bg-gray-100 dark:bg-gray-700 rounded px-2 py-1 text-sm border-0 outline-none w-24"
          />

          <input
            type="text"
            placeholder="Director"
            value={criteria.director || ''}
            onChange={(e) => onUpdate({ director: e.target.value || undefined })}
            className="bg-gray-100 dark:bg-gray-700 rounded px-2 py-1 text-sm border-0 outline-none w-24"
          />

          <input
            type="text"
            placeholder="Cast"
            value={criteria.cast || ''}
            onChange={(e) => onUpdate({ cast: e.target.value || undefined })}
            className="bg-gray-100 dark:bg-gray-700 rounded px-2 py-1 text-sm border-0 outline-none w-24"
          />
        </div>
      </div>
    </div>
  );
}
