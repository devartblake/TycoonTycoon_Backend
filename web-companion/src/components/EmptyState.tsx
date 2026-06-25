/**
 * Reusable empty state component for pages with no content
 */

interface EmptyStateProps {
  icon: string | React.ReactNode;
  title: string;
  description: string;
  action?: {
    label: string;
    onClick: () => void;
  };
}

export function EmptyState({ icon, title, description, action }: EmptyStateProps) {
  return (
    <div
      className="p-12 rounded-lg text-center"
      style={{ backgroundColor: 'var(--color-bg-secondary)' }}
    >
      {/* Icon */}
      <div className="text-6xl mb-4 flex justify-center">
        {typeof icon === 'string' ? icon : icon}
      </div>

      {/* Title */}
      <h3
        className="text-xl font-bold mb-2"
        style={{ color: 'var(--color-text-primary)' }}
      >
        {title}
      </h3>

      {/* Description */}
      <p
        className="text-sm mb-6 max-w-sm mx-auto"
        style={{ color: 'var(--color-text-secondary)' }}
      >
        {description}
      </p>

      {/* Action Button */}
      {action && (
        <button
          onClick={action.onClick}
          className="px-6 py-2 rounded-lg font-semibold transition-all"
          style={{
            backgroundColor: 'var(--color-brand-primary)',
            color: 'white',
          }}
        >
          {action.label}
        </button>
      )}
    </div>
  );
}

export default EmptyState;
