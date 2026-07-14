/**
 * Shown when a nav feature is feature-flagged off (installer, diagnostics, etc.).
 */

import { Link } from 'react-router-dom'

interface FeatureUnavailableProps {
  title: string
  reason: string
  alternatives?: { label: string; href?: string; note?: string }[]
}

export default function FeatureUnavailable({
  title,
  reason,
  alternatives = [],
}: FeatureUnavailableProps) {
  return (
    <div className="operator-container max-w-xl mx-auto py-16 space-y-6 text-center">
      <h1 className="text-2xl font-bold text-ink-primary">{title}</h1>
      <p className="text-ink-secondary text-sm leading-relaxed">{reason}</p>
      {alternatives.length > 0 && (
        <ul className="text-left text-sm text-ink-secondary space-y-2 bg-bg-secondary rounded p-4 border border-panel-border">
          {alternatives.map((a) => (
            <li key={a.label}>
              <span className="font-medium text-ink-primary">{a.label}</span>
              {a.note ? <span className="block text-ink-tertiary text-xs mt-0.5">{a.note}</span> : null}
              {a.href ? (
                <Link to={a.href} className="text-accent text-xs hover:underline">
                  Open
                </Link>
              ) : null}
            </li>
          ))}
        </ul>
      )}
      <Link
        to="/dashboard"
        className="inline-block text-sm text-accent hover:underline"
      >
        ← Back to dashboard
      </Link>
    </div>
  )
}
