/**
 * Question card component - displays the current question
 */

import { AlertCircle } from 'lucide-react';
import type { Question } from '@stores/quizSessionStore';

interface QuestionCardProps {
  question: Question;
  currentIndex: number;
  totalQuestions: number;
}

export function QuestionCard({
  question,
  currentIndex,
  totalQuestions,
}: QuestionCardProps) {
  return (
    <div
      className="rounded-lg p-8 mb-8"
      style={{ backgroundColor: 'var(--color-bg-secondary)' }}
    >
      {/* Progress */}
      <div className="mb-6">
        <div className="flex items-center justify-between mb-2">
          <span
            className="text-sm font-medium"
            style={{ color: 'var(--color-text-secondary)' }}
          >
            Question {currentIndex + 1} of {totalQuestions}
          </span>
          <span
            className="text-sm px-3 py-1 rounded-full"
            style={{
              backgroundColor: 'var(--color-bg-tertiary)',
              color: 'var(--color-text-secondary)',
            }}
          >
            {question.difficulty.toUpperCase()}
          </span>
        </div>
        <div className="w-full h-2 rounded-full" style={{ backgroundColor: 'var(--color-bg-tertiary)' }}>
          <div
            className="h-2 rounded-full transition-all duration-300"
            style={{
              width: `${((currentIndex + 1) / totalQuestions) * 100}%`,
              backgroundColor: 'var(--color-brand-primary)',
            }}
          />
        </div>
      </div>

      {/* Category */}
      <div className="mb-4">
        <span
          className="text-xs font-semibold uppercase tracking-wide"
          style={{ color: 'var(--color-brand-primary)' }}
        >
          {question.category}
        </span>
      </div>

      {/* Question Text */}
      <h2
        className="text-2xl font-bold mb-8 leading-relaxed"
        style={{ color: 'var(--color-text-primary)' }}
      >
        {question.question}
      </h2>

      {/* Time Limit Indicator */}
      <div className="flex items-center gap-2 text-sm">
        <AlertCircle size={16} style={{ color: 'var(--color-status-info)' }} />
        <span style={{ color: 'var(--color-text-secondary)' }}>
          {question.timeLimit} seconds to answer
        </span>
      </div>
    </div>
  );
}

export default QuestionCard;
