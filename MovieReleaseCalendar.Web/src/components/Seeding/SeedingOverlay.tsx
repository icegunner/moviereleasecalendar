import { useState, useEffect, useCallback } from 'react';
import Spinner from '../common/Spinner';
import { getStatus } from '../../api/scraperApi';

interface SeedingOverlayProps {
  onSeeded: () => void;
}

export default function SeedingOverlay({ onSeeded }: SeedingOverlayProps) {
  const [visible, setVisible] = useState(false);
  const [message, setMessage] = useState('Checking status...');

  const poll = useCallback(async () => {
    try {
      const status = await getStatus();
      if (status.isSeeding) {
        setVisible(true);
        setMessage(
          status.movieCount > 0
            ? `Setting up... ${status.movieCount} movies imported so far.`
            : 'Setting up for the first time — scraping movie data...'
        );
      } else {
        if (visible) {
          // Was seeding, now done
          setVisible(false);
          onSeeded();
        }
      }
    } catch {
      // Ignore network errors during polling
    }
  }, [visible, onSeeded]);

  useEffect(() => {
    // Initial check
    poll();

    const interval = setInterval(poll, 3000);
    return () => clearInterval(interval);
  }, [poll]);

  if (!visible) return null;

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-gray-900/90 backdrop-blur-sm">
      <div className="text-center space-y-6">
        <div className="flex justify-center">
          <Spinner size="lg" />
        </div>
        <div>
          <h2 className="text-2xl font-bold text-white mb-2">Movie Release Calendar</h2>
          <p className="text-gray-300 text-sm max-w-md">{message}</p>
        </div>
        <div className="w-48 mx-auto bg-gray-700 rounded-full h-1.5 overflow-hidden">
          <div className="h-full bg-primary-500 rounded-full animate-pulse" style={{ width: '60%' }} />
        </div>
      </div>
    </div>
  );
}
