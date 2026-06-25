/**
 * Quiz lobby - allows user to select difficulty and category before starting
 */

import { useNavigate } from 'react-router-dom';
import { useQuizSessionStore } from '@stores/quizSessionStore';
import { getQuestionsByCategory, getAllCategories } from '@lib/mockQuestions';
import { Play, Star, Zap } from 'lucide-react';

export function QuizLobbyPage() {
  const navigate = useNavigate();
  const startQuiz = useQuizSessionStore((state) => state.startQuiz);

  const difficulties = [
    { id: 'easy', label: '⭐ Easy', description: 'Perfect for beginners', icon: '😊' },
    { id: 'medium', label: '⭐⭐ Medium', description: 'Test your knowledge', icon: '🎯' },
    { id: 'hard', label: '⭐⭐⭐ Hard', description: 'Challenge yourself', icon: '🔥' },
  ] as const;

  const categories = getAllCategories().map((cat) => ({
    id: cat,
    label: cat.charAt(0).toUpperCase() + cat.slice(1),
  }));

  const handleStartQuiz = (
    difficulty: 'easy' | 'medium' | 'hard',
    category: string
  ) => {
    const questions = getQuestionsByCategory(category, 5);
    startQuiz(questions, category, difficulty);
    navigate('/quiz/session');
  };

  return (
    <div className="p-8 min-h-screen" style={{ backgroundColor: 'var(--color-bg-primary)' }}>
      <div className="max-w-4xl mx-auto">
        <div className="mb-12">
          <h1
            className="text-4xl font-bold mb-2"
            style={{ color: 'var(--color-text-primary)' }}
          >
            Start Quiz
          </h1>
          <p style={{ color: 'var(--color-text-secondary)' }}>
            Choose your difficulty and category to begin!
          </p>
        </div>

        {/* Category Selection */}
        <div className="mb-12">
          <h2
            className="text-2xl font-bold mb-4"
            style={{ color: 'var(--color-text-primary)' }}
          >
            Select Category
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {categories.map((cat) => (
              <div
                key={cat.id}
                className="p-6 rounded-lg cursor-pointer transition-all hover:scale-105"
                style={{
                  backgroundColor: 'var(--color-bg-secondary)',
                  border: '2px solid var(--color-ui-border)',
                }}
              >
                <h3
                  className="text-lg font-semibold mb-2"
                  style={{ color: 'var(--color-text-primary)' }}
                >
                  {cat.label}
                </h3>
                <p style={{ color: 'var(--color-text-secondary)', fontSize: '0.875rem' }}>
                  5 questions
                </p>

                {/* Difficulty selection for this category */}
                <div className="mt-4 space-y-2">
                  {difficulties.map((diff) => (
                    <button
                      key={diff.id}
                      onClick={() => handleStartQuiz(diff.id, cat.id)}
                      className="w-full py-2 px-3 rounded-lg text-sm font-medium transition-all flex items-center gap-2"
                      style={{
                        backgroundColor: 'var(--color-bg-tertiary)',
                        color: 'var(--color-text-primary)',
                      }}
                    >
                      <Play size={16} />
                      {diff.label}
                    </button>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Quick Start Cards */}
        <div className="mb-12">
          <h2
            className="text-2xl font-bold mb-4"
            style={{ color: 'var(--color-text-primary)' }}
          >
            Quick Start
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {/* Easy */}
            <button
              onClick={() => handleStartQuiz('easy', 'science')}
              className="p-8 rounded-lg transition-all hover:scale-105 border-2"
              style={{
                backgroundColor: 'var(--color-bg-secondary)',
                borderColor: 'var(--color-status-success)',
              }}
            >
              <Star
                size={32}
                style={{ color: 'var(--color-status-success)', marginBottom: '1rem' }}
              />
              <h3
                className="text-lg font-bold mb-2"
                style={{ color: 'var(--color-text-primary)' }}
              >
                Easy Quiz
              </h3>
              <p style={{ color: 'var(--color-text-secondary)', marginBottom: '1rem' }}>
                Science • 5 Questions
              </p>
              <span
                className="text-sm font-semibold"
                style={{ color: 'var(--color-status-success)' }}
              >
                Start →
              </span>
            </button>

            {/* Medium */}
            <button
              onClick={() => handleStartQuiz('medium', 'history')}
              className="p-8 rounded-lg transition-all hover:scale-105 border-2"
              style={{
                backgroundColor: 'var(--color-bg-secondary)',
                borderColor: 'var(--color-status-warning)',
              }}
            >
              <Zap
                size={32}
                style={{ color: 'var(--color-status-warning)', marginBottom: '1rem' }}
              />
              <h3
                className="text-lg font-bold mb-2"
                style={{ color: 'var(--color-text-primary)' }}
              >
                Medium Quiz
              </h3>
              <p style={{ color: 'var(--color-text-secondary)', marginBottom: '1rem' }}>
                History • 5 Questions
              </p>
              <span
                className="text-sm font-semibold"
                style={{ color: 'var(--color-status-warning)' }}
              >
                Start →
              </span>
            </button>

            {/* Hard */}
            <button
              onClick={() => handleStartQuiz('hard', 'literature')}
              className="p-8 rounded-lg transition-all hover:scale-105 border-2"
              style={{
                backgroundColor: 'var(--color-bg-secondary)',
                borderColor: 'var(--color-status-error)',
              }}
            >
              <span style={{ fontSize: '2rem', marginBottom: '1rem', display: 'block' }}>🔥</span>
              <h3
                className="text-lg font-bold mb-2"
                style={{ color: 'var(--color-text-primary)' }}
              >
                Hard Quiz
              </h3>
              <p style={{ color: 'var(--color-text-secondary)', marginBottom: '1rem' }}>
                Literature • 5 Questions
              </p>
              <span
                className="text-sm font-semibold"
                style={{ color: 'var(--color-status-error)' }}
              >
                Start →
              </span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

export default QuizLobbyPage;
