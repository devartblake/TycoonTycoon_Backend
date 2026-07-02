/**
 * Content API client
 */

import { apiGet, apiPost } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type { QuestionsListResponse, QuestionFilter, QuestionReview, QuestionsStats } from './types'

export async function getQuestions(filters?: QuestionFilter, offset: number = 0, limit: number = 50): Promise<QuestionsListResponse> {
  if (getMockMode()) return mockApi.mockGetQuestions(filters, offset, limit)
  const params = new URLSearchParams({
    offset: offset.toString(),
    limit: limit.toString(),
    ...Object.fromEntries(
      Object.entries(filters || {}).filter(([, v]) => v != null).map(([k, v]) => [k, String(v)])
    ),
  })
  return apiGet(`/admin/content/questions?${params}`)
}

export async function getQuestionDetail(questionId: string): Promise<any> {
  if (getMockMode()) return mockApi.mockGetQuestionDetail(questionId)
  return apiGet(`/admin/content/questions/${questionId}`)
}

export async function reviewQuestion(review: QuestionReview): Promise<{ success: boolean; nextQuestionId?: string }> {
  if (getMockMode()) return mockApi.mockReviewQuestion(review)
  return apiPost(`/admin/content/questions/${review.questionId}/review`, {
    verdict: review.verdict,
    reason: review.reason,
    notes: review.notes,
  })
}

export async function bulkReviewQuestions(questionIds: string[], verdict: 'approve' | 'reject', reason?: string): Promise<{ success: boolean; reviewed: number }> {
  if (getMockMode()) return mockApi.mockBulkReviewQuestions(questionIds, verdict, reason)
  return apiPost('/admin/content/questions/bulk-review', {
    questionIds,
    verdict,
    reason,
  })
}

export async function getQuestionsStats(): Promise<QuestionsStats> {
  if (getMockMode()) return mockApi.mockGetQuestionsStats()
  return apiGet('/admin/content/questions/stats')
}

export async function getCategories(): Promise<string[]> {
  if (getMockMode()) return mockApi.mockGetCategories()
  return apiGet('/admin/content/categories')
}
