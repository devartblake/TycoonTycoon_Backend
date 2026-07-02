/**
 * Content feature types
 */

export interface Question {
  id: string
  text: string
  category: string
  difficulty: 'easy' | 'medium' | 'hard'
  answers: Answer[]
  correctAnswerId: string
  explanation?: string
  source: string
  status: 'pending' | 'approved' | 'rejected'
  rejectionReason?: string
  submittedBy: string
  submittedAt: string
  reviewedBy?: string
  reviewedAt?: string
  tags?: string[]
}

export interface Answer {
  id: string
  text: string
  isCorrect: boolean
}

export interface QuestionsListResponse {
  items: Question[]
  total: number
  offset: number
  limit: number
}

export interface QuestionFilter {
  status?: 'pending' | 'approved' | 'rejected'
  category?: string
  difficulty?: 'easy' | 'medium' | 'hard'
  searchText?: string
}

export interface QuestionReview {
  questionId: string
  verdict: 'approve' | 'reject'
  reason?: string
  notes?: string
}

export interface QuestionsStats {
  totalPending: number
  totalApproved: number
  totalRejected: number
  approvalRate: number
  avgReviewTime: number // minutes
}
