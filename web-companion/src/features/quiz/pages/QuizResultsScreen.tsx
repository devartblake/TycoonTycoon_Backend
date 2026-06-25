/**
 * Quiz results/summary screen
 * Shows final score, XP, accuracy, and breakdown
 */

import { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import type { QuizSessionStats } from '@stores/quizSessionStore';
import { useQuizSessionStore } from '@stores/quizSessionStore';
import { useProfileStore } from '@stores/profileStore';
import { Trophy, Zap, Percent, Clock, ArrowRight, AlertCircle } from 'lucide-react';
import { apiClient } from '@core/api/client';

export function QuizResultsScreen() {
  const navigate = useNavigate();
  const location = useLocation();
  const stats = location.state as QuizSessionStats | null;
  const sessionId = useQuizSessionStore((state) => state.sessionId);
  const addXP = useProfileStore((state) => state.addXP);
  const addCoins = useProfileStore((state) => state.addCoins);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  // Submit results to the API
  useEffect(() => {
    const submitResults = async () => {
      if (!stats) return;

      try {
        setIsSubmitting(true);
        setSubmitError(null);

        // Convert quiz session stats to API format
        // Note: selectedAnswerIndex is the array index, we convert it to option ID format
        const answers = stats.answers.map((answer) => ({
          questionId: answer.questionId,
          selectedOptionId: `option_${answer.selectedAnswer}`, // Convert index to option ID
        }));

        // Submit to API
        const response = await apiClient.submitMatchResults(
          sessionId || 'unknown',
          answers,
          stats.totalScore
        );

        // Update player profile with earned rewards
        if (response) {
          addXP(stats.totalXp);
          addCoins(Math.floor(stats.totalScore / 10)); // Convert score to coins
        }
      } catch (err) {
        console.error('Failed to submit quiz results:', err);
        setSubmitError('Failed to save your results. Your progress will still be recorded.');
      } finally {
        setIsSubmitting(false);
      }
    };

    submitResults();
  }, [stats, sessionId, addXP, addCoins]);

  if (!stats) {
    return (
      <div className="p-8 flex items-center justify-center min-h-screen">
        <div style={{ color: 'var(--color-text-secondary)' }}>
          No results found. <button onClick={() => navigate('/play')} className="underline">Back to Quiz Lobby</button>
        </div>
      </div>
    );
  }

  if (isSubmitting) {
    return (
      <div className="p-8 flex items-center justify-center min-h-screen" style={{ backgroundColor: 'var(--color-bg-primary)' }}>
        <div className="text-center">
          <div className="inline-block mb-4">
            <div
              className="w-8 h-8 border-4 border-transparent rounded-full animate-spin"
              style={{ borderTopColor: 'var(--color-brand-primary)' }}
            />
          </div>
          <p style={{ color: 'var(--color-text-secondary)' }}>Saving your results...</p>
        </div>
      </div>
    );
  }

  const getPerformanceLevel = (accuracy: number) => {
    if (accuracy === 100) return { label: 'Perfect! 🤩', color: 'var(--color-status-success)' };
    if (accuracy >= 90) return { label: 'Excellent! 🌟', color: 'var(--color-status-success)' };
    if (accuracy >= 80) return { label: 'Very Good! 😊', color: 'var(--color-brand-primary)' };
    if (accuracy >= 70) return { label: 'Good! 👍', color: 'var(--color-brand-secondary)' };
    if (accuracy >= 60) return { label: 'Okay! 📚', color: 'var(--color-status-warning)' };
    return { label: 'Keep Learning 💪', color: 'var(--color-status-error)' };
  };

  const performance = getPerformanceLevel(stats.accuracy);

  return (
    <div
      className="min-h-screen p-8"
      style={{ backgroundColor: 'var(--color-bg-primary)' }}
    >
      <div className="max-w-2xl mx-auto">
        {/* Performance Badge */}
        <div className="text-center mb-12">
          <div
            className="inline-flex items-center justify-center w-24 h-24 rounded-full mb-6"
            style={{ backgroundColor: performance.color }}
          >
            <Trophy size={48} color="white" />
          </div>
          <h1
            className="text-4xl font-bold mb-2"
            style={{ color: performance.color }}
          >
            {performance.label}
          </h1>
          <p style={{ color: 'var(--color-text-secondary)' }}>
            Quiz completed in {Math.floor(stats.timeSpent / 60)}m{stats.timeSpent % 60}s
          </p>
        </div>

        {/* Submission Error Alert */}
        {submitError && (
          <div
            className="mb-8 p-6 rounded-lg flex items-start gap-3"
            style={{
              backgroundColor: 'var(--color-status-error)',
              color: 'white',
            }}
          >
            <AlertCircle size={24} className="flex-shrink-0" />
            <div>
              <h3 className="font-bold mb-1">Couldn't save results</h3>
              <p>{submitError}</p>
            </div>
          </div>
        )}

        {/* Main Stats Grid */}
        <div className="grid grid-cols-2 gap-4 mb-8">
          {/* Score */}
          <div
            className="rounded-lg p-6"
            style={{ backgroundColor: 'var(--color-bg-secondary)' }}
          >
            <div className="flex items-center gap-3 mb-3">
              <Trophy size={24} style={{ color: 'var(--color-brand-primary)' }} />
              <span style={{ color: 'var(--color-text-secondary)' }}>Score</span>
            </div>
            <div
              className="text-3xl font-bold"
              style={{ color: 'var(--color-text-primary)' }}
            >
              {stats.totalScore}
            </div>
            <p style={{ color: 'var(--color-text-tertiary)', fontSize: '0.875rem' }}>
              points earned
            </p>
          </div>

          {/* XP */}
          <div
            className="rounded-lg p-6"
            style={{ backgroundColor: 'var(--color-bg-secondary)' }}
          >
            <div className="flex items-center gap-3 mb-3">
              <Zap size={24} style={{ color: 'var(--color-brand-accent)' }} />
              <span style={{ color: 'var(--color-text-secondary)' }}>Experience</span>
            </div>
            <div
              className="text-3xl font-bold"
              style={{ color: 'var(--color-text-primary)' }}
            >
              +{stats.totalXp}
            </div>
            <p style={{ color: 'var(--color-text-tertiary)', fontSize: '0.875rem' }}>
              XP gained
            </p>
          </div>

          {/* Accuracy */}
          <div
            className="rounded-lg p-6"
            style={{ backgroundColor: 'var(--color-bg-secondary)' }}
          >
            <div className="flex items-center gap-3 mb-3">
              <Percent size={24} style={{ color: 'var(--color-status-success)' }} />
              <span style={{ color: 'var(--color-text-secondary)' }}>Accuracy</span>
            </div>
            <div
              className="text-3xl font-bold"
              style={{ color: 'var(--color-text-primary)' }}
            >
              {stats.accuracy.toFixed(1)}%
            </div>
            <p style={{ color: 'var(--color-text-tertiary)', fontSize: '0.875rem' }}>
              {stats.correctAnswers}/{stats.totalQuestions} correct
            </p>
          </div>

          {/* Time */}
          <div
            className="rounded-lg p-6"
            style={{ backgroundColor: 'var(--color-bg-secondary)' }}
          >
            <div className="flex items-center gap-3 mb-3">
              <Clock size={24} style={{ color: 'var(--color-status-info)' }} />
              <span style={{ color: 'var(--color-text-secondary)' }}>Time</span>
            </div>
            <div
              className="text-3xl font-bold"
              style={{ color: 'var(--color-text-primary)' }}
            >
              {Math.floor(stats.timeSpent / 60)}:{String(stats.timeSpent % 60).padStart(2, '0')}
            </div>
            <p style={{ color: 'var(--color-text-tertiary)', fontSize: '0.875rem' }}>
              minutes spent
            </p>
          </div>
        </div>

        {/* Category Breakdown */}
        <div
          className="rounded-lg p-6 mb-8"
          style={{ backgroundColor: 'var(--color-bg-secondary)' }}
        >
          <h2
            className="text-lg font-bold mb-4"
            style={{ color: 'var(--color-text-primary)' }}
          >
            Category Performance
          </h2>
          <div className="space-y-4">
            {/* Single category breakdown */}
            <div className="flex items-center justify-between p-4 rounded-lg" style={{ backgroundColor: 'var(--color-bg-tertiary)' }}>
              <div>
                <h3
                  className="font-semibold mb-1"
                  style={{ color: 'var(--color-text-primary)' }}
                >
                  {stats.categoryStats.category}
                </h3>
                <p style={{ color: 'var(--color-text-secondary)', fontSize: '0.875rem' }}>
                  {stats.categoryStats.correct}/{stats.categoryStats.total} questions correct
                </p>
              </div>
              <div className="text-right">
                <div
                  className="text-2xl font-bold"
                  style={{ color: 'var(--color-text-primary)' }}
                >
                  {stats.categoryStats.accuracy.toFixed(0)}%
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Bonus Section */}
        {stats.totalXp > stats.totalQuestions * 100 && (
          <div
            className="rounded-lg p-6 mb-8 border-2"
            style={{
              backgroundColor: 'var(--color-bg-secondary)',
              borderColor: 'var(--color-status-success)',
            }}
          >
            <h3
              className="font-bold mb-2"
              style={{ color: 'var(--color-status-success)' }}
            >
              🎉 Bonus XP Earned!
            </h3>
            <p style={{ color: 'var(--color-text-secondary)' }}>
              You earned {stats.totalXp - stats.totalQuestions * 100} bonus XP for your performance!
            </p>
          </div>
        )}

        {/* Action Buttons */}
        <div className="flex gap-4">
          <button
            onClick={() => navigate('/play')}
            className="flex-1 py-3 px-6 rounded-lg font-semibold transition-all flex items-center justify-center gap-2"
            style={{
              backgroundColor: 'var(--color-brand-primary)',
              color: 'white',
            }}
          >
            <span>Play Again</span>
            <ArrowRight size={20} />
          </button>
          <button
            onClick={() => navigate('/')}
            className="flex-1 py-3 px-6 rounded-lg font-semibold transition-all"
            style={{
              backgroundColor: 'var(--color-bg-secondary)',
              color: 'var(--color-text-primary)',
              border: '2px solid var(--color-ui-border)',
            }}
          >
            Back to Dashboard
          </button>
        </div>
      </div>
    </div>
  );
}

export default QuizResultsScreen;
