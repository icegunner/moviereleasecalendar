import { useState, useEffect } from 'react';
import Modal from '../common/Modal';
import { usePreferences } from '../../hooks/usePreferences';
import { describeCron, cronPresets } from '../../utils/cronDescriptions';
import { runScraper } from '../../api/scraperApi';
import Spinner from '../common/Spinner';
import { QuestionMarkCircleIcon } from '@heroicons/react/24/outline';
import type { CalendarViewType, UserPreferences } from '../../types';

interface SettingsPanelProps {
  open: boolean;
  onClose: () => void;
}

export default function SettingsPanel({ open, onClose }: SettingsPanelProps) {
  const { prefs, updatePrefs } = usePreferences();
  const [draft, setDraft] = useState<UserPreferences | null>(null);
  const [saving, setSaving] = useState(false);
  const [scraping, setScraping] = useState(false);
  const [scrapeResult, setScrapeResult] = useState<string | null>(null);
  const [cronMode, setCronMode] = useState<'preset' | 'custom'>('preset');

  useEffect(() => {
    if (prefs) {
      setDraft({ ...prefs });
      const isPreset = cronPresets.some((p) => p.value === prefs.cronSchedule);
      setCronMode(isPreset ? 'preset' : 'custom');
    }
  }, [prefs, open]);

  if (!draft) return null;

  const handleSave = async () => {
    setSaving(true);
    try {
      await updatePrefs(draft);
      onClose();
    } catch (err) {
      console.error('Failed to save preferences', err);
    } finally {
      setSaving(false);
    }
  };

  const handleRunScraper = async () => {
    setScraping(true);
    setScrapeResult(null);
    try {
      const result = await runScraper();
      setScrapeResult(`Done! ${result.imported} new, ${result.updated} updated.`);
    } catch (err) {
      setScrapeResult('Scraper failed. Check server logs.');
      console.error(err);
    } finally {
      setScraping(false);
    }
  };

  return (
    <Modal open={open} onClose={onClose} title="Settings" wide>
      <div className="space-y-6">
        {/* Appearance */}
        <section>
          <h3 className="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">
            Appearance
          </h3>
          <div className="space-y-3">
            <label className="flex items-center justify-between">
              <span className="text-sm">Theme</span>
              <select
                value={draft.theme}
                onChange={(e) => setDraft({ ...draft, theme: e.target.value })}
                className="bg-gray-100 dark:bg-gray-700 rounded px-3 py-1.5 text-sm border-0 outline-none"
              >
                <option value="dark">Dark</option>
                <option value="light">Light</option>
              </select>
            </label>
            <label className="flex items-center justify-between">
              <span className="text-sm">Default View</span>
              <select
                value={draft.defaultView}
                onChange={(e) =>
                  setDraft({ ...draft, defaultView: e.target.value as CalendarViewType })
                }
                className="bg-gray-100 dark:bg-gray-700 rounded px-3 py-1.5 text-sm border-0 outline-none"
              >
                <option value="dayGridMonth">Month</option>
                <option value="dayGridWeek">Week</option>
                <option value="yearGrid">Year</option>
              </select>
            </label>
            <label className="flex items-center justify-between">
              <span className="text-sm">Show Ratings</span>
              <input
                type="checkbox"
                checked={draft.showRatings}
                onChange={(e) => setDraft({ ...draft, showRatings: e.target.checked })}
                className="h-4 w-4 rounded text-primary-500"
              />
            </label>
          </div>
        </section>

        {/* TMDb API Key */}
        <section>
          <h3 className="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">
            TMDb API Key
          </h3>
          <input
            type="password"
            value={draft.tmdbApiKey}
            onChange={(e) => setDraft({ ...draft, tmdbApiKey: e.target.value })}
            placeholder="Enter TMDb API key..."
            className="w-full bg-gray-100 dark:bg-gray-700 rounded px-3 py-2 text-sm border-0 outline-none"
          />
        </section>

        {/* Scraping Schedule */}
        <section>
          <h3 className="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">
            Scraping Schedule
          </h3>
          <div className="space-y-2">
            <div className="flex gap-2">
              <button
                onClick={() => setCronMode('preset')}
                className={`px-3 py-1 text-xs rounded ${cronMode === 'preset' ? 'bg-primary-500 text-white' : 'bg-gray-100 dark:bg-gray-700'}`}
              >
                Presets
              </button>
              <button
                onClick={() => setCronMode('custom')}
                className={`px-3 py-1 text-xs rounded ${cronMode === 'custom' ? 'bg-primary-500 text-white' : 'bg-gray-100 dark:bg-gray-700'}`}
              >
                Custom
              </button>
              {cronMode === 'custom' && (
                <div className="relative group inline-block ml-1">
                  <QuestionMarkCircleIcon className="h-4 w-4 text-gray-400 dark:text-gray-500 cursor-help" />
                  <div className="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-72 p-3 bg-gray-800 dark:bg-gray-700 text-white text-xs rounded-lg shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-50">
                    <p className="font-semibold mb-1">Cron Expression Format</p>
                    <p className="mb-2 text-gray-300">A cron expression has 5 fields:<br/>
                      <code className="text-primary-300">minute hour day-of-month month day-of-week</code>
                    </p>
                    <p className="font-semibold mb-1">Examples:</p>
                    <ul className="space-y-0.5 text-gray-300">
                      <li><code className="text-primary-300">0 0 * * 0</code> — Every Sunday at midnight</li>
                      <li><code className="text-primary-300">0 0 * * *</code> — Every day at midnight</li>
                      <li><code className="text-primary-300">0 6 * * 1</code> — Every Monday at 6 AM</li>
                      <li><code className="text-primary-300">0 */6 * * *</code> — Every 6 hours</li>
                      <li><code className="text-primary-300">0 0 1 * *</code> — First day of each month</li>
                    </ul>
                    <div className="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-gray-800 dark:border-t-gray-700"></div>
                  </div>
                </div>
              )}
            </div>
            {cronMode === 'preset' ? (
              <select
                value={draft.cronSchedule}
                onChange={(e) => setDraft({ ...draft, cronSchedule: e.target.value })}
                className="w-full bg-gray-100 dark:bg-gray-700 rounded px-3 py-2 text-sm border-0 outline-none"
              >
                {cronPresets.map((p) => (
                  <option key={p.value} value={p.value}>
                    {p.label}
                  </option>
                ))}
              </select>
            ) : (
              <input
                type="text"
                value={draft.cronSchedule}
                onChange={(e) => setDraft({ ...draft, cronSchedule: e.target.value })}
                placeholder="0 0 * * 0"
                className="w-full bg-gray-100 dark:bg-gray-700 rounded px-3 py-2 text-sm font-mono border-0 outline-none"
              />
            )}
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {describeCron(draft.cronSchedule)}
            </p>
          </div>
        </section>

        {/* Swagger */}
        <section>
          <h3 className="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">
            Developer
          </h3>
          <label className="flex items-center justify-between">
            <span className="text-sm">Enable Swagger UI</span>
            <input
              type="checkbox"
              checked={draft.enableSwagger}
              onChange={(e) => setDraft({ ...draft, enableSwagger: e.target.checked })}
              className="h-4 w-4 rounded text-primary-500"
            />
          </label>
        </section>

        {/* Actions */}
        <section>
          <h3 className="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">
            Actions
          </h3>
          <div className="flex items-center gap-3">
            <button
              onClick={handleRunScraper}
              disabled={scraping}
              className="px-4 py-2 text-sm font-medium rounded-lg bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 disabled:opacity-50 transition-colors flex items-center gap-2"
            >
              {scraping && <Spinner size="sm" />}
              {scraping ? 'Scraping...' : 'Run Scraper Now'}
            </button>
            {scrapeResult && (
              <span className="text-xs text-gray-500 dark:text-gray-400">{scrapeResult}</span>
            )}
          </div>
        </section>

        {/* Save / Cancel */}
        <div className="flex justify-end gap-2 pt-4 border-t border-gray-200 dark:border-gray-700">
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm rounded-lg bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={handleSave}
            disabled={saving}
            className="px-4 py-2 text-sm font-medium rounded-lg bg-primary-500 text-white hover:bg-primary-600 disabled:opacity-50 transition-colors flex items-center gap-2"
          >
            {saving && <Spinner size="sm" />}
            Save
          </button>
        </div>
      </div>
    </Modal>
  );
}
