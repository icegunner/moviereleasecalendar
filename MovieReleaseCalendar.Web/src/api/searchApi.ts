import type { SearchCriteria, MovieSearchResult } from '../types';

const BASE = '';

export async function searchMovies(
  criteria: SearchCriteria
): Promise<MovieSearchResult[]> {
  const params = new URLSearchParams();
  if (criteria.q) params.set('q', criteria.q);
  if (criteria.genre) params.set('genre', criteria.genre);
  if (criteria.director) params.set('director', criteria.director);
  if (criteria.cast) params.set('cast', criteria.cast);
  if (criteria.rating) params.set('rating', criteria.rating);
  if (criteria.year) params.set('year', String(criteria.year));
  if (criteria.month) params.set('month', String(criteria.month));
  if (criteria.imdbId) params.set('imdbId', criteria.imdbId);
  const url = `${BASE}/api/movies/search?${params}`;
  const res = await fetch(url);
  if (!res.ok) throw new Error(`Search failed: ${res.status}`);
  return res.json();
}

export async function getMovieById(id: string): Promise<MovieSearchResult> {
  const res = await fetch(`${BASE}/api/movies/${encodeURIComponent(id)}`);
  if (!res.ok) throw new Error(`Movie not found: ${res.status}`);
  return res.json();
}
