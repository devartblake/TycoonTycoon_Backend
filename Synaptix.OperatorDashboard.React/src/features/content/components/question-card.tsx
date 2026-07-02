/**
 * Question card for review
 */

import type { Question } from '../types'

interface QuestionCardProps {
  question: Question | null
  isLoading: boolean
}

const DIFFICULTY_CONFIG = {
  easy: { color: 'text-status-healthy', bg: 'bg-status-healthy/10', label: 'Easy' },
  medium: { color: 'text-status-degraded', bg: 'bg-status-degraded/10', label: 'Medium' },
  hard: { color: 'text-status-offline', bg: 'bg-status-offline/10', label: 'Hard' },
}

const STATUS_CONFIG = {
  pending: { color: 'text-ink-secondary', bg: 'bg-ink-secondary/10', label: 'Pending' },
  approved: { color: 'text-status-healthy', bg: 'bg-status-healthy/10', label: 'Approved' },
  rejected: { color: 'text-status-offline', bg: 'bg-status-offline/10', label: 'Rejected' },
}

export function QuestionCard({ question, isLoading }: QuestionCardProps) {
  if (isLoading) {
    return (
      <div className="operator-card space-y-4">
        {[...Array(4)].map((_, i) => (
          <div key={i} className="h-12 bg-bg-secondary rounded animate-pulse" />
        ))}
      </div>
    )
  }

  if (!question) {
    return (
      <div className="text-center py-12 text-ink-secondary operator-card">
        <p>No question to review</p>
      </div>
    )
  }

  const diffConfig = DIFFICULTY_CONFIG[question.difficulty]
  const statusConfig = STATUS_CONFIG[question.status]

  return (
    <div className="operator-card space-y-6">
      {/* Header */}
      <div className="space-y-3">
        <div className="flex items-start justify-between gap-4">
          <div className="flex-1">
            <h2 className="text-xl font-semibold text-ink-primary leading-relaxed">{question.text}</h2>
          </div>
          <div className="flex gap-2">
            <span className={`px-3 py-1 rounded text-xs font-medium whitespace-nowrap ${diffConfig.bg} ${diffConfig.color}`}>
              {diffConfig.label}
            </span>
            <span className={`px-3 py-1 rounded text-xs font-medium whitespace-nowrap ${statusConfig.bg} ${statusConfig.color}`}>
              {statusConfig.label}
            </span>
          </div>
        </div>

        <div className="flex items-center gap-3 text-sm text-ink-secondary">
          <span className="px-2 py-1 bg-bg-secondary rounded">{question.category}</span>
          {question.tags && question.tags.length > 0 && (
            <div className="flex gap-1">
              {question.tags.map((tag) => (
                <span key={tag} className="px-2 py-1 bg-bg-secondary rounded text-xs">
                  #{tag}
                </span>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Answers */}
      <div className="space-y-2 pt-4 border-t border-panel-border">
        <p className="text-sm font-medium text-ink-tertiary">Answers:</p>
        <div className="space-y-2">
          {question.answers.map((answer) => (
            <div
              key={answer.id}
              className={`p-3 rounded border-l-4 ${
                answer.isCorrect
                  ? 'border-status-healthy bg-status-healthy/5'
                  : 'border-panel-border bg-bg-secondary'
              }`}
            >
              <div className="flex items-start gap-3">
                <span className="text-lg">
                  {answer.isCorrect ? '✓' : '○'}
                </span>
                <p className="text-sm text-ink-primary">{answer.text}</p>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Explanation */}
      {question.explanation && (
        <div className="pt-4 border-t border-panel-border">
          <p className="text-sm font-medium text-ink-tertiary mb-2">Explanation:</p>
          <p className="text-sm text-ink-secondary leading-relaxed">{question.explanation}</p>
        </div>
      )}

      {/* Metadata */}
      <div className="pt-4 border-t border-panel-border space-y-2 text-xs text-ink-tertiary">
        <p>Submitted by: <span className="text-ink-secondary font-medium">{question.submittedBy}</span></p>
        <p>Submitted: {new Date(question.submittedAt).toLocaleString()}</p>
        {question.reviewedBy && (
          <p>Reviewed by: <span className="text-ink-secondary font-medium">{question.reviewedBy}</span></p>
        )}
        {question.rejectionReason && (
          <p className="text-status-offline">Rejection: {question.rejectionReason}</p>
        )}
      </div>
    </div>
  )
}
