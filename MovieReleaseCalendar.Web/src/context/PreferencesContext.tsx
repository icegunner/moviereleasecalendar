import { createContext, useContext, useState, useEffect, useCallback, type ReactNode } from 'react';
import type { UserPreferences } from '../types';
import { getPreferences, savePreferences } from '../api/preferencesApi';

interface PreferencesContextValue {
  prefs: UserPreferences | null;
  loading: boolean;
  updatePrefs: (updated: UserPreferences) => Promise<void>;
  refresh: () => Promise<void>;
}

const PreferencesContext = createContext<PreferencesContextValue>({
  prefs: null,
  loading: true,
  updatePrefs: async () => {},
  refresh: async () => {},
});

export function PreferencesProvider({ children }: { children: ReactNode }) {
  const [prefs, setPrefs] = useState<UserPreferences | null>(null);
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    try {
      const p = await getPreferences();
      setPrefs(p);
    } catch (err) {
      console.error('Failed to load preferences', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const updatePrefs = useCallback(async (updated: UserPreferences) => {
    const saved = await savePreferences(updated);
    setPrefs(saved);
  }, []);

  return (
    <PreferencesContext.Provider value={{ prefs, loading, updatePrefs, refresh }}>
      {children}
    </PreferencesContext.Provider>
  );
}

export function usePreferencesContext() {
  return useContext(PreferencesContext);
}
