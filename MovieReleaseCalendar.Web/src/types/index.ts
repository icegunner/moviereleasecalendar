// ── Domain Models ──

export interface TrailerLink {
  name: string;
  url: string;
  site: string;
  publishedAt?: string;
}

export interface Movie {
  id: string;
  title: string;
  releaseDate: string;
  url: string;
  description: string;
  genres: string[];
  posterUrl: string;
  tmdbId: number;
  imdbId: string;
  directors: string[];
  cast: string[];
  mpaaRating: string;
  trailers: TrailerLink[];
}

export interface MovieCalendarEvent {
  title: string;
  date: string;
  description: string;
  url: string;
  posterUrl: string;
  allDay: boolean;
  tmdbId: number;
  imdbId: string;
  mpaaRating: string;
  genres: string[];
  directors: string[];
  cast: string[];
  trailers: TrailerLink[];
}

export interface MovieSearchResult {
  id: string;
  title: string;
  releaseDate: string;
  url: string;
  posterUrl: string;
  genres: string[];
  mpaaRating: string;
  imdbId: string;
  directors: string[];
  cast: string[];
  description: string;
  trailers: TrailerLink[];
}

export interface SearchCriteria {
  q?: string;
  genre?: string;
  director?: string;
  cast?: string;
  rating?: string;
  year?: number;
  month?: number;
  imdbId?: string;
}

export interface UserPreferences {
  id: string;
  theme: string;
  defaultView: string;
  tmdbApiKey: string;
  cronSchedule: string;
  showRatings: boolean;
  enableSwagger: boolean;
  updatedAt: string;
}

export interface AppStatus {
  status: string;
  isSeeding: boolean;
  movieCount: number;
}

export interface ScrapeResponse {
  imported: number;
  updated: number;
}

export type CalendarViewType = 'dayGridMonth' | 'dayGridWeek' | 'yearGrid';
