/** MPAA rating → Tailwind color class mapping */
export function getRatingColor(rating: string): string {
  switch (rating?.toUpperCase()) {
    case 'G':
    case 'PG':
      return 'bg-green-500 text-white';
    case 'PG-13':
      return 'bg-amber-500 text-white';
    case 'R':
    case 'NC-17':
      return 'bg-red-500 text-white';
    default:
      return 'bg-gray-400 text-white';
  }
}

/** MPAA rating → hex color for FullCalendar event backgrounds */
export function getRatingHex(rating: string): string {
  switch (rating?.toUpperCase()) {
    case 'G':
    case 'PG':
      return '#22c55e';
    case 'PG-13':
      return '#eab308';
    case 'R':
    case 'NC-17':
      return '#ef4444';
    default:
      return '#6b7280';
  }
}
