import { useState } from 'react';
import {
  MagnifyingGlassIcon,
  Cog6ToothIcon,
  SunIcon,
  MoonIcon,
} from '@heroicons/react/24/outline';
import { useTheme } from '../../hooks/useTheme';
import type { CalendarViewType } from '../../types';

interface HeaderProps {
  currentView: CalendarViewType;
  onViewChange: (view: CalendarViewType) => void;
  onSearchToggle: () => void;
  onSettingsToggle: () => void;
}

const viewButtons: { label: string; value: CalendarViewType }[] = [
  { label: 'Year', value: 'yearGrid' },
  { label: 'Month', value: 'dayGridMonth' },
  { label: 'Week', value: 'dayGridWeek' },
];

export default function Header({
  currentView,
  onViewChange,
  onSearchToggle,
  onSettingsToggle,
}: HeaderProps) {
  const { theme, toggleTheme } = useTheme();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  return (
    <header className="sticky top-0 z-40 bg-white/95 dark:bg-gray-900/95 backdrop-blur border-b border-gray-200 dark:border-gray-700">
      <div className="px-4 py-2 flex items-center gap-2">
        {/* Left: Logo + Title — fixed width to balance with right actions */}
        <div className="flex items-center gap-2 min-w-0 w-40 sm:w-56 flex-shrink-0">
          <img src="/logo.png" alt="Logo" className="h-8 w-8 object-contain" onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }} />
          <h1 className="text-lg font-bold truncate hidden sm:block">Movie Release Calendar</h1>
          <h1 className="text-lg font-bold truncate sm:hidden">MRC</h1>
        </div>

        {/* Center: View toggle — flex-1 to center between logo and actions */}
        <div className="flex-1 hidden md:flex justify-center">
          <div className="inline-flex items-center bg-gray-100 dark:bg-gray-800 rounded-lg p-0.5">
            {viewButtons.map((btn) => (
              <button
                key={btn.value}
                onClick={() => onViewChange(btn.value)}
                className={`px-3 py-1 text-sm font-medium rounded-md transition-colors ${
                  currentView === btn.value
                    ? 'bg-primary-500 text-white shadow'
                    : 'text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white'
                }`}
              >
                {btn.label}
              </button>
            ))}
          </div>
        </div>

        {/* Right: Actions — fixed width to balance with left logo */}
        <div className="flex items-center gap-1 w-40 sm:w-56 flex-shrink-0 justify-end">
          <button
            onClick={onSearchToggle}
            className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
            title="Search"
          >
            <MagnifyingGlassIcon className="h-5 w-5" />
          </button>
          <button
            onClick={toggleTheme}
            className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
            title={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
          >
            {theme === 'dark' ? (
              <SunIcon className="h-5 w-5 text-yellow-400" />
            ) : (
              <MoonIcon className="h-5 w-5" />
            )}
          </button>
          <button
            onClick={onSettingsToggle}
            className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
            title="Settings"
          >
            <Cog6ToothIcon className="h-5 w-5" />
          </button>

          {/* Mobile hamburger for view toggle */}
          <button
            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            className="md:hidden p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
            title="Views"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
        </div>
      </div>

      {/* Mobile view selector dropdown */}
      {mobileMenuOpen && (
        <div className="md:hidden border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 px-4 py-2 flex gap-2">
          {viewButtons.map((btn) => (
            <button
              key={btn.value}
              onClick={() => {
                onViewChange(btn.value);
                setMobileMenuOpen(false);
              }}
              className={`flex-1 px-3 py-1.5 text-sm font-medium rounded-md transition-colors ${
                currentView === btn.value
                  ? 'bg-primary-500 text-white shadow'
                  : 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300'
              }`}
            >
              {btn.label}
            </button>
          ))}
        </div>
      )}
    </header>
  );
}
