import { useEffect, useCallback } from 'react';
import { usePreferencesContext } from '../context/PreferencesContext';

export function useTheme() {
  const { prefs, updatePrefs } = usePreferencesContext();
  const theme = prefs?.theme ?? 'dark';

  useEffect(() => {
    const root = document.documentElement;
    if (theme === 'dark') {
      root.classList.add('dark');
    } else {
      root.classList.remove('dark');
    }
  }, [theme]);

  const toggleTheme = useCallback(async () => {
    if (!prefs) return;
    const newTheme = theme === 'dark' ? 'light' : 'dark';
    await updatePrefs({ ...prefs, theme: newTheme });
  }, [prefs, theme, updatePrefs]);

  return { theme, toggleTheme };
}
