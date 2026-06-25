/**
 * Quiz lobby - allows user to select difficulty and category before starting
 */

import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuizSessionStore } from '@stores/quizSessionStore';
import { apiClient } from '@core/api/client';
import { Play, Star, Zap, AlertCircle } from 'lucide-react';
import { GridSkeleton } from '@components/skeletons/GridSkeleton';
import { EmptyState } from '@components/EmptyState';
import { PageTransition } from '@components/PageTransition';
import { useToast } from '@hooks/useToast';

export function QuizLobbyPage() {
  const navigate = useNavigate();
  const toast = useToast();
  const startQuiz = useQuizSessionStore((state) => state.startQuiz);
  const setSessionId = useQuizSessionStore((state) => state.setSessionId);
  const [categories, setCategories] = useState<Array<{ id: string; label: string }>>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchCategories = async () => {
      try {
        setIsLoading(true);
        setError(null);
        const fetchedCategories = await apiClient.getQuestionCategories();
        // Convert API response to our format
        const formatted = Object.entries(fetchedCategories).map(([id]) => ({
          id,
          label: id.charAt(0).toUpperCase() + id.slice(1),
        }));
        setCategories(formatted);
      } catch (err) {
        console.error('Failed to fetch categories:', err);
        const errorMsg = 'Failed to load categories. Please try again.';
        setError(errorMsg);
        toast.error(errorMsg);
      } finally {
        setIsLoading(false);
      }
    };

    fetchCategories();
  }, [toast]);

  const difficulties = [
    { id: 'easy', label: '⭐ Easy', description: 'Perfect for beginners', icon: '😊' },
    { id: 'medium', label: '⭐⭐ Medium', description: 'Test your knowledge', icon: '🎯' },
    { id: 'hard', label: '⭐⭐⭐ Hard', description: 'Challenge yourself', icon: '🔥' },
  ] as const;

  const handleStartQuiz = async (
    difficulty: 'easy' | 'medium' | 'hard',
    category: string
  ) => {
    try {
      setIsLoading(true);
      setError(null);

      // 1. Create a match session on the backend
      const matchResponse = await apiClient.startMatch('single');
      const matchId = matchResponse.matchId || matchResponse.id;

      // 2. Fetch questions from API
      const questions = await apiClient.getQuizQuestions(category, difficulty);

      // 3. Start quiz session with match ID
      startQuiz(questions, category, difficulty);
      setSessionId(matchId);

      toast.success('Quiz started! Ready to play?');
      navigate('/quiz/session');
    } catch (err) {
      console.error('Failed to start quiz:', err);
      const errorMsg = 'Failed to start quiz. Please try again.';
      setError(errorMsg);
      toast.error(errorMsg);
    } finally {
      setIsLoading(false);
    }
  };

  if (error) {
    return (
      <PageTransition>
        <div className="p-8 min-h-screen" style={{ backgroundColor: 'var(--color-bg-primary)' }}>
          <div className="max-w-4xl mx-auto">
            <div
              className="p-6 rounded-lg flex items-start gap-3"
              style={{
                backgroundColor: 'var(--color-status-error)',
                color: 'white',
              }}
            >
              <AlertCircle size={24} className="flex-shrink-0" />
              <div>
                <h3 className="font-bold mb-1">Error Loading Quiz</h3>
                <p>{error}</p>
                <button
                  onClick={() => navigate('/')}
                  className="mt-4 px-4 py-2 rounded-lg"
                  style={{ backgroundColor: 'rgba(255, 255, 255, 0.2)' }}
                >
                  Back to Dashboard
                </button>
              </div>
            </div>
          </div>
        </div>
      </PageTransition>
    );
  }

  return (
    <PageTransition>
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
          {isLoading ? (
            <GridSkeleton items={4} columns={2} />
          ) : categories.length === 0 ? (
            <EmptyState
              icon="📚"
              title="No Categories Available"
              description="Quiz categories are currently unavailable. Please check back soon!"
              action={{
                label: 'Refresh',
                onClick: () => {
                  setIsLoading(true);
                  setError(null);
                  apiClient.getQuestionCategories()
                    .then(data => {
                      const formatted = Object.entries(data).map(([id]) => ({
                        id,
                        label: id.charAt(0).toUpperCase() + id.slice(1),
                      }));
                      setCategories(formatted);
                    })
                    .catch(err => {
                      console.error('Failed to fetch categories:', err);
                      const errorMsg = 'Failed to load categories. Please try again.';
                      setError(errorMsg);
                      toast.error(errorMsg);
                    })
                    .finally(() => setIsLoading(false));
                },
              }}
            />
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {categories.map((cat) => (
                <div
                  key={cat.id}
                  className="p-6 rounded-lg transition-all"
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
                        disabled={isLoading}
                        className="w-full py-2 px-3 rounded-lg text-sm font-medium transition-all flex items-center gap-2 disabled:opacity-50"
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
          )}
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
    </PageTransition>
  );
}

export default QuizLobbyPage;
