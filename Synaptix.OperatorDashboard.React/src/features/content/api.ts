/**
 * Questions API — Django admin_questions_client + AdminQuestionsEndpoints.
 *
 *   GET    /admin/questions
 *   POST   /admin/questions
 *   GET    /admin/questions/{id}
 *   PATCH  /admin/questions/{id}
 *   DELETE /admin/questions/{id}
 *   POST   /admin/questions/{id}/approve|reject
 *   POST   /admin/questions/bulk              (import)
 *   POST   /admin/questions/bulk-review
 *   GET    /admin/questions/stats
 *   GET    /admin/questions/categories
 *
 * List DTO omits full options; detail is fully populated.
 */

import { apiGet, apiPost, apiPatch, apiDelete } from '@/lib/api-client'
import { getMockMode } from '@/lib/api-config'
import * as mockApi from '@/lib/mock-api-client'
import type { QuestionsListResponse, QuestionFilter, QuestionReview, QuestionsStats, Question, Answer } from './types'

interface BackendQuestionOption {
  id: string
  text: string
}

interface BackendQuestionDto {
  id: string
  text: string
  category: string
  difficulty: number | string
  status: string
  options: BackendQuestionOption[]
  correctOptionId: string
  tags: string[]
  mediaKey?: string | null
  mediaUrl?: string | null
  createdAtUtc: string
  updatedAtUtc: string
}

interface BackendQuestionListItem {
  id: string
  textPreview: string
  category: string
  difficulty: number | string
  tags: string[]
  updatedAtUtc: string
}

interface BackendQuestionListResponse {
  items: BackendQuestionListItem[]
  total: number
  page: number
  pageSize: number
}

function offsetToPage(offset: number, limit: number): number {
  return Math.floor(offset / Math.max(1, limit)) + 1
}

function mapDifficulty(difficulty: number | string): Question['difficulty'] {
  const d = typeof difficulty === 'string' ? difficulty.toLowerCase() : difficulty
  if (d === 1 || d === 'easy') return 'easy'
  if (d === 3 || d === 4 || d === 'hard' || d === 'expert') return 'hard'
  return 'medium'
}

function mapStatus(status: string | undefined): Question['status'] {
  switch ((status ?? '').toLowerCase()) {
    case 'approved':
      return 'approved'
    case 'rejected':
      return 'rejected'
    default:
      return 'pending'
  }
}

function detailToQuestion(dto: BackendQuestionDto): Question {
  const answers: Answer[] = (dto.options ?? []).map((o) => ({
    id: o.id,
    text: o.text,
    isCorrect: o.id === dto.correctOptionId,
  }))
  return {
    id: dto.id,
    text: dto.text,
    category: dto.category,
    difficulty: mapDifficulty(dto.difficulty),
    answers,
    correctAnswerId: dto.correctOptionId,
    source: '',
    status: mapStatus(dto.status),
    submittedBy: '',
    submittedAt: dto.createdAtUtc,
    tags: dto.tags,
  }
}

function listItemToQuestion(item: BackendQuestionListItem, status: Question['status']): Question {
  return {
    id: item.id,
    text: item.textPreview,
    category: item.category,
    difficulty: mapDifficulty(item.difficulty),
    answers: [],
    correctAnswerId: '',
    source: '',
    status,
    submittedBy: '',
    submittedAt: item.updatedAtUtc,
    tags: item.tags,
  }
}

export async function getQuestions(filters?: QuestionFilter, offset: number = 0, limit: number = 50): Promise<QuestionsListResponse> {
  if (getMockMode()) return mockApi.mockGetQuestions(filters, offset, limit)
  const params = new URLSearchParams({
    page: offsetToPage(offset, limit).toString(),
    pageSize: limit.toString(),
  })
  if (filters?.status) params.set('status', filters.status)
  if (filters?.category) params.set('category', filters.category)
  if (filters?.searchText) params.set('q', filters.searchText)
  const res = await apiGet<BackendQuestionListResponse>(`/admin/questions?${params}`)
  // The list DTO omits status; if a status filter was applied, every item matches it.
  const inferredStatus: Question['status'] = filters?.status ?? 'pending'
  return {
    items: res.items.map((i) => listItemToQuestion(i, inferredStatus)),
    total: res.total,
    offset,
    limit,
  }
}

export async function getQuestionDetail(questionId: string): Promise<Question> {
  if (getMockMode()) return mockApi.mockGetQuestionDetail(questionId)
  const dto = await apiGet<BackendQuestionDto>(`/admin/questions/${questionId}`)
  return detailToQuestion(dto)
}

export async function reviewQuestion(review: QuestionReview): Promise<{ success: boolean; nextQuestionId?: string }> {
  if (getMockMode()) return mockApi.mockReviewQuestion(review)
  const verb = review.verdict === 'approve' ? 'approve' : 'reject'
  await apiPost(`/admin/questions/${review.questionId}/${verb}`, {})
  return { success: true }
}

export async function bulkReviewQuestions(questionIds: string[], verdict: 'approve' | 'reject', _reason?: string): Promise<{ success: boolean; reviewed: number }> {
  if (getMockMode()) return mockApi.mockBulkReviewQuestions(questionIds, verdict, _reason)
  void _reason
  return apiPost<{ success: boolean; reviewed: number }>('/admin/questions/bulk-review', { ids: questionIds, verdict })
}

export async function getQuestionsStats(): Promise<QuestionsStats> {
  if (getMockMode()) return mockApi.mockGetQuestionsStats()
  return apiGet<QuestionsStats>('/admin/questions/stats')
}

export async function getCategories(): Promise<string[]> {
  if (getMockMode()) return mockApi.mockGetCategories()
  return apiGet<string[]>('/admin/questions/categories')
}

/** Django create_question */
export async function createQuestion(payload: Record<string, unknown>): Promise<Question> {
  if (getMockMode()) {
    return {
      id: `q_${Date.now()}`,
      text: String(payload.text ?? ''),
      category: String(payload.category ?? ''),
      difficulty: 'medium',
      answers: [],
      correctAnswerId: '',
      source: '',
      status: 'pending',
      submittedBy: '',
      submittedAt: new Date().toISOString(),
      tags: [],
    }
  }
  const dto = await apiPost<BackendQuestionDto>('/admin/questions', payload)
  return detailToQuestion(dto)
}

/** Django update_question */
export async function updateQuestion(
  questionId: string,
  payload: Record<string, unknown>
): Promise<Question> {
  if (getMockMode()) return getQuestionDetail(questionId)
  const dto = await apiPatch<BackendQuestionDto>(`/admin/questions/${questionId}`, payload)
  return detailToQuestion(dto)
}

/** Django delete_question */
export async function deleteQuestion(questionId: string): Promise<{ success: boolean }> {
  if (getMockMode()) return { success: true }
  await apiDelete(`/admin/questions/${questionId}`)
  return { success: true }
}

/** Django bulk_import_questions → POST /admin/questions/bulk */
export async function bulkImportQuestions(
  questions: Record<string, unknown>[]
): Promise<{ success: boolean; imported?: number }> {
  if (getMockMode()) return { success: true, imported: questions.length }
  const res = await apiPost<{ imported?: number; count?: number }>('/admin/questions/bulk', {
    questions,
  })
  return { success: true, imported: res.imported ?? res.count ?? questions.length }
}
