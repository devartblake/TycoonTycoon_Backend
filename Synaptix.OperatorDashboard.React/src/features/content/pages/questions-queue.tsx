/**
 * Questions Queue - Content Moderation
 */

import { useState } from 'react'
import { usePermission } from '@/hooks/use-permission'
import { QuestionCard } from '../components/question-card'
import { ReviewPanel } from '../components/review-panel'
import { FilterBar } from '../components/filter-bar'
import {
  useQuestions,
  useQuestionsStats,
  useReviewQuestion,
} from '../hooks/useContent'
import type { QuestionFilter } from '../types'

export default function QuestionsQueuePage() {
  usePermission('content:write')

  const [filters, setFilters] = useState<QuestionFilter>({ status: 'pending' })
  const [offset, setOffset] = useState(0)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const limit = 50

  const questionsQuery = useQuestions(filters, offset, limit)
  const statsQuery = useQuestionsStats()
  const reviewMutation = useReviewQuestion()

  const questions = questionsQuery.data?.items || []
  const currentQuestion = questions.length > 0 ? questions[0] : null

  const handleApprove = async (reason?: string, notes?: string) => {
    if (!currentQuestion) return
    await reviewMutation.mutateAsync({
      questionId: currentQuestion.id,
      verdict: 'approve',
      reason,
      notes,
    })
    setSuccessMessage('Question approved')
    setTimeout(() => setSuccessMessage(null), 2000)
  }

  const handleReject = async (reason: string, notes?: string) => {
    if (!currentQuestion) return
    await reviewMutation.mutateAsync({
      questionId: currentQuestion.id,
      verdict: 'reject',
      reason,
      notes,
    })
    setSuccessMessage('Question rejected')
    setTimeout(() => setSuccessMessage(null), 2000)
  }

  return (
    <div className="operator-container space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-ink-primary">Questions Queue</h1>
        <p className="mt-2 text-ink-secondary">Review and moderate game questions</p>
      </div>

      {/* Stats */}
      {statsQuery.data && (
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Pending</p>
            <p className="text-2xl font-bold text-accent mt-1">
              {statsQuery.data.totalPending}
            </p>
          </div>
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Approved</p>
            <p className="text-2xl font-bold text-status-healthy mt-1">
              {statsQuery.data.totalApproved}
            </p>
          </div>
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Rejected</p>
            <p className="text-2xl font-bold text-status-offline mt-1">
              {statsQuery.data.totalRejected}
            </p>
          </div>
          <div className="operator-card">
            <p className="text-xs text-ink-tertiary">Approval Rate</p>
            <p className="text-2xl font-bold text-status-healthy mt-1">
              {Math.round(statsQuery.data.approvalRate * 100)}%
            </p>
          </div>
        </div>
      )}

      {/* Success Message */}
      {successMessage && (
        <div className="p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm">
          ✓ {successMessage}
        </div>
      )}

      {/* Main Content */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Left: Filters */}
        <div>
          <FilterBar filters={filters} onFiltersChange={(newFilters) => {
            setFilters(newFilters)
            setOffset(0)
          }} />
        </div>

        {/* Center: Question Card */}
        <div className="lg:col-span-2">
          <QuestionCard
            question={currentQuestion}
            isLoading={questionsQuery.isLoading}
          />

          {/* Pagination */}
          {questionsQuery.data && questionsQuery.data.total > limit && (
            <div className="mt-4 flex justify-between">
              <button
                onClick={() => setOffset(Math.max(0, offset - limit))}
                disabled={offset === 0}
                className="px-3 py-1 text-xs bg-bg-secondary border border-panel-border rounded hover:bg-bg-tertiary disabled:opacity-50 disabled:cursor-not-allowed"
              >
                ← Previous
              </button>
              <p className="text-xs text-ink-secondary">
                Page {Math.floor(offset / limit) + 1} of {Math.ceil(questionsQuery.data.total / limit)}
              </p>
              <button
                onClick={() => setOffset(offset + limit)}
                disabled={offset + limit >= questionsQuery.data.total}
                className="px-3 py-1 text-xs bg-bg-secondary border border-panel-border rounded hover:bg-bg-tertiary disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Next →
              </button>
            </div>
          )}
        </div>

        {/* Right: Review Panel */}
        {currentQuestion && (
          <div>
            <ReviewPanel
              onApprove={handleApprove}
              onReject={handleReject}
              isLoading={reviewMutation.isPending}
            />
          </div>
        )}
      </div>

      {/* Empty State */}
      {!questionsQuery.isLoading && questions.length === 0 && (
        <div className="text-center py-12 text-ink-secondary">
          <p className="text-lg">✅ Queue is clear!</p>
          <p className="text-sm mt-2">All {filters.status || 'matching'} questions have been reviewed.</p>
        </div>
      )}
    </div>
  )
}
