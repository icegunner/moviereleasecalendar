import type { UserPreferences } from '../types';

const BASE = '';

export async function getPreferences(): Promise<UserPreferences> {
  const res = await fetch(`${BASE}/api/preferences`);
  if (!res.ok) throw new Error(`Failed to fetch preferences: ${res.status}`);
  return res.json();
}

export async function savePreferences(
  prefs: UserPreferences
): Promise<UserPreferences> {
  const res = await fetch(`${BASE}/api/preferences`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(prefs),
  });
  if (!res.ok) throw new Error(`Failed to save preferences: ${res.status}`);
  return res.json();
}
