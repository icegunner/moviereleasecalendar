import { useState, useCallback, useRef, useEffect } from 'react';
import type { SearchCriteria, MovieSearchResult } from '../types';
import { searchMovies } from '../api/searchApi';

export function useSearch() {
  const [results, setResults] = useState<MovieSearchResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [criteria, setCriteria] = useState<SearchCriteria>({});
  const debounceRef = useRef<ReturnType<typeof setTimeout>>();

  const doSearch = useCallback(async (c: SearchCriteria) => {
    // Don't fire a search when all criteria are empty
    const hasAnyCriteria = Object.values(c).some((v) => v !== undefined && v !== '' && v !== 0);
    if (!hasAnyCriteria) {
      setResults([]);
      return;
    }
    setLoading(true);
    try {
      const data = await searchMovies(c);
      setResults(data);
    } catch (err) {
      console.error('Search failed', err);
      setResults([]);
    } finally {
      setLoading(false);
    }
  }, []);

  const updateCriteria = useCallback(
    (patch: Partial<SearchCriteria>) => {
      setCriteria((prev) => {
        const next = { ...prev, ...patch };
        // Debounce text search
        if (debounceRef.current) clearTimeout(debounceRef.current);
        debounceRef.current = setTimeout(() => doSearch(next), 300);
        return next;
      });
    },
    [doSearch]
  );

  const reset = useCallback(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    setCriteria({});
    setResults([]);
  }, []);

  // Cleanup
  useEffect(() => {
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  }, []);

  return { results, loading, criteria, updateCriteria, doSearch, reset };
}
