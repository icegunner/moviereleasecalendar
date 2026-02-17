import type { AppStatus, ScrapeResponse } from '../types';

const BASE = '';

export async function getStatus(): Promise<AppStatus> {
  const res = await fetch(`${BASE}/api/status`);
  if (!res.ok) throw new Error(`Failed to fetch status: ${res.status}`);
  return res.json();
}

export async function runScraper(years?: number[]): Promise<ScrapeResponse> {
  const res = await fetch(`${BASE}/api/scraper/run`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ years: years ?? [] }),
  });
  if (!res.ok) throw new Error(`Scraper failed: ${res.status}`);
  return res.json();
}
