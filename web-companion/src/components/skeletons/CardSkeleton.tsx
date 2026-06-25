/**
 * Reusable card skeleton loader for loading states
 */

export function CardSkeleton() {
  return (
    <div
      className="rounded-lg p-6 space-y-4"
      style={{ backgroundColor: 'var(--color-bg-secondary)' }}
    >
      {/* Header skeleton */}
      <div className="flex items-start justify-between">
        <div className="flex-1 space-y-2">
          <div
            className="h-6 w-3/4 rounded animate-pulse"
            style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
          />
          <div
            className="h-4 w-1/2 rounded animate-pulse"
            style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
          />
        </div>
        <div
          className="h-12 w-12 rounded animate-pulse"
          style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
        />
      </div>

      {/* Content skeleton */}
      <div className="space-y-3">
        <div
          className="h-4 w-full rounded animate-pulse"
          style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
        />
        <div
          className="h-4 w-5/6 rounded animate-pulse"
          style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
        />
      </div>

      {/* Footer skeleton */}
      <div className="flex gap-2 pt-2">
        <div
          className="h-10 flex-1 rounded animate-pulse"
          style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
        />
        <div
          className="h-10 w-10 rounded animate-pulse"
          style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
        />
      </div>
    </div>
  );
}

export default CardSkeleton;
