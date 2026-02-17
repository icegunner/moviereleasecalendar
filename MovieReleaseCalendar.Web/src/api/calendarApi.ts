import type { MovieCalendarEvent } from '../types';

const BASE = '';

export async function fetchCalendarEvents(
  start?: string,
  end?: string
): Promise<MovieCalendarEvent[]> {
  const params = new URLSearchParams();
  if (start) params.set('start', start);
  if (end) params.set('end', end);
  const url = `${BASE}/api/calendar/events.json${params.toString() ? '?' + params : ''}`;
  const res = await fetch(url);
  if (!res.ok) throw new Error(`Failed to fetch events: ${res.status}`);
  return res.json();
}

export async function fetchIcsUrl(): Promise<string> {
  return `${BASE}/calendar.ics`;
}
