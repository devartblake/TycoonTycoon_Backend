/**
 * Reusable grid skeleton loader for grid layouts
 */

interface GridSkeletonProps {
  items?: number;
  columns?: 1 | 2 | 3 | 4;
}

export function GridSkeleton({ items = 6, columns = 3 }: GridSkeletonProps) {
  const gridColsClass = {
    1: 'grid-cols-1',
    2: 'md:grid-cols-2',
    3: 'md:grid-cols-3',
    4: 'md:grid-cols-4',
  }[columns];

  return (
    <div className={`grid grid-cols-1 ${gridColsClass} gap-4`}>
      {Array.from({ length: items }).map((_, idx) => (
        <div
          key={idx}
          className="rounded-lg overflow-hidden"
          style={{ backgroundColor: 'var(--color-bg-secondary)' }}
        >
          {/* Image skeleton */}
          <div
            className="h-40 w-full animate-pulse"
            style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
          />

          {/* Content skeleton */}
          <div className="p-4 space-y-3">
            <div
              className="h-5 w-3/4 rounded animate-pulse"
              style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
            />
            <div
              className="h-4 w-full rounded animate-pulse"
              style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
            />
            <div
              className="h-4 w-2/3 rounded animate-pulse"
              style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
            />

            {/* Footer skeleton */}
            <div className="flex gap-2 pt-2">
              <div
                className="h-10 flex-1 rounded animate-pulse"
                style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
              />
              <div
                className="h-10 flex-1 rounded animate-pulse"
                style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
              />
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

export default GridSkeleton;
