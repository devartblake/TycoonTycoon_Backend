interface EmptyStateProps {
  title: string
  description?: string
  icon?: string
  action?: {
    label: string
    onClick: () => void
  }
}

export default function EmptyState({
  title,
  description,
  icon = '📭',
  action,
}: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center py-12 px-4">
      <div className="text-4xl mb-4">{icon}</div>
      <h3 className="text-lg font-semibold text-ink-primary mb-2">{title}</h3>
      {description && (
        <p className="text-ink-secondary text-sm mb-4 text-center max-w-sm">{description}</p>
      )}
      {action && (
        <button
          onClick={action.onClick}
          className="mt-4 px-4 py-2 bg-accent text-white rounded hover:bg-accent-dark"
        >
          {action.label}
        </button>
      )}
    </div>
  )
}

export function EmptyCard({
  title,
  description,
  icon = '📭',
  action,
}: EmptyStateProps) {
  return (
    <div className="operator-card">
      <EmptyState title={title} description={description} icon={icon} action={action} />
    </div>
  )
}
