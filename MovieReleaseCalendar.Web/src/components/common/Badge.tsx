import { getRatingColor } from '../../utils/ratingColors';

interface BadgeProps {
  rating: string;
  className?: string;
}

export default function Badge({ rating, className = '' }: BadgeProps) {
  if (!rating) return null;
  return (
    <span
      className={`inline-flex items-center px-1.5 py-0.5 rounded text-xs font-bold ${getRatingColor(rating)} ${className}`}
    >
      {rating}
    </span>
  );
}
