/**
 * Reusable table skeleton loader for lists
 */

interface TableSkeletonProps {
  rows?: number;
  columns?: number;
}

export function TableSkeleton({ rows = 5, columns = 5 }: TableSkeletonProps) {
  return (
    <div
      className="rounded-lg overflow-hidden border"
      style={{
        backgroundColor: 'var(--color-bg-secondary)',
        borderColor: 'var(--color-ui-border)',
      }}
    >
      {/* Header */}
      <div
        className="flex p-4 border-b gap-4"
        style={{
          backgroundColor: 'var(--color-bg-tertiary)',
          borderColor: 'var(--color-ui-border)',
        }}
      >
        {Array.from({ length: columns }).map((_, idx) => (
          <div
            key={idx}
            className="h-4 flex-1 rounded animate-pulse"
            style={{ backgroundColor: 'var(--color-bg-secondary)' }}
          />
        ))}
      </div>

      {/* Rows */}
      {Array.from({ length: rows }).map((_, rowIdx) => (
        <div
          key={rowIdx}
          className="flex p-4 border-b gap-4"
          style={{
            backgroundColor: rowIdx % 2 === 0 ? 'transparent' : 'rgba(0,0,0,0.1)',
            borderColor: 'var(--color-ui-border)',
          }}
        >
          {Array.from({ length: columns }).map((_, colIdx) => (
            <div
              key={colIdx}
              className="h-4 flex-1 rounded animate-pulse"
              style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
            />
          ))}
        </div>
      ))}
    </div>
  );
}

export default TableSkeleton;
