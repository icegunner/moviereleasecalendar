import cronstrue from 'cronstrue';

/**
 * Convert a cron expression to a human-readable description.
 * Returns the raw expression if parsing fails.
 */
export function describeCron(expression: string): string {
  try {
    return cronstrue.toString(expression, { use24HourTimeFormat: false });
  } catch {
    return expression;
  }
}

/** Common cron presets for the settings UI */
export const cronPresets: { label: string; value: string }[] = [
  { label: 'Daily at midnight', value: '0 0 * * *' },
  { label: 'Every Sunday at midnight', value: '0 0 * * 0' },
  { label: 'Every Monday at midnight', value: '0 0 * * 1' },
  { label: 'Twice a week (Sun & Wed)', value: '0 0 * * 0,3' },
  { label: 'Monthly (1st at midnight)', value: '0 0 1 * *' },
];
