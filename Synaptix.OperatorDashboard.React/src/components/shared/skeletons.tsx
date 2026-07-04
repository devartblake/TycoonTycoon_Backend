export function SkeletonCard() {
  return (
    <div className="operator-card animate-pulse">
      <div className="space-y-3 p-4">
        <div className="h-4 bg-panel rounded w-3/4"></div>
        <div className="h-8 bg-panel rounded w-1/2"></div>
      </div>
    </div>
  )
}

export function SkeletonGrid({ count = 4 }: { count?: number }) {
  return (
    <div className="grid grid-cols-4 gap-4">
      {Array.from({ length: count }).map((_, i) => (
        <SkeletonCard key={i} />
      ))}
    </div>
  )
}

export function SkeletonTableRow({ columns = 5 }: { columns?: number }) {
  return (
    <tr className="border-t border-panel-border animate-pulse">
      {Array.from({ length: columns }).map((_, i) => (
        <td key={i} className="px-4 py-3">
          <div className="h-4 bg-panel rounded"></div>
        </td>
      ))}
    </tr>
  )
}

export function SkeletonTable({ rows = 5, columns = 5 }: { rows?: number; columns?: number }) {
  return (
    <div className="operator-card">
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-panel">
            <tr>
              {Array.from({ length: columns }).map((_, i) => (
                <th key={i} className="px-4 py-2">
                  <div className="h-4 bg-panel-border rounded w-3/4"></div>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {Array.from({ length: rows }).map((_, i) => (
              <SkeletonTableRow key={i} columns={columns} />
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

export function SkeletonChart({ height = 'h-48' }: { height?: string }) {
  return (
    <div className={`operator-card animate-pulse ${height}`}>
      <div className="p-4 h-full bg-panel rounded"></div>
    </div>
  )
}

export function SkeletonList({ items = 5 }: { items?: number }) {
  return (
    <div className="operator-card space-y-2 p-4">
      {Array.from({ length: items }).map((_, i) => (
        <div key={i} className="p-3 border border-panel-border rounded animate-pulse">
          <div className="h-4 bg-panel rounded w-3/4 mb-2"></div>
          <div className="h-3 bg-panel rounded w-1/2"></div>
        </div>
      ))}
    </div>
  )
}

export function SkeletonHeader() {
  return (
    <div className="space-y-4 animate-pulse">
      <div className="h-8 bg-panel rounded w-1/3"></div>
      <div className="h-4 bg-panel rounded w-1/2"></div>
    </div>
  )
}

export function SkeletonDetail() {
  return (
    <div className="operator-card space-y-4 p-6 animate-pulse">
      {Array.from({ length: 4 }).map((_, i) => (
        <div key={i} className="space-y-2">
          <div className="h-4 bg-panel rounded w-1/4"></div>
          <div className="h-5 bg-panel rounded w-1/2"></div>
        </div>
      ))}
    </div>
  )
}
