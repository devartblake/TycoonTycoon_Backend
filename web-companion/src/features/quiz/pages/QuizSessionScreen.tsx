/**
 * Main quiz game session screen
 * Displays question, timer, answers, and manages game flow
 */

import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuizSessionStore } from '@stores/quizSessionStore';
import { QuestionCard } from '@components/game/QuestionCard';
import { AnswerButton } from '@components/game/AnswerButton';
import { TimerBar } from '@components/game/TimerBar';
import { BarChart3, Zap } from 'lucide-react';

export function QuizSessionScreen() {
  const navigate = useNavigate();
  const { sessionId } = useParams();
  const [selectedAnswer, setSelectedAnswer] = useState<number | null>(null);
  const [isRevealed, setIsRevealed] = useState(false);

  const {
    questions,
    currentQuestionIndex,
    category,
    difficulty,
    setCurrentQuestion,
    answerQuestion,
    timeRemaining,
    setTimeRemaining,
    score,
    xpEarned,
    completeQuiz,
  } = useQuizSessionStore();

  // Initialize quiz if not already started
  useEffect(() => {
    if (questions.length === 0) {
      navigate('/play'); // Redirect if no quiz session
    }
  }, []);

  // Timer countdown
  useEffect(() => {
    if (!isRevealed && questions.length > 0) {
      const timer = setInterval(() => {
        setTimeRemaining(Math.max(0, timeRemaining - 1));

        // Auto-submit when time runs out
        if (timeRemaining <= 1) {
          if (selectedAnswer !== null) {
            submitAnswer();
          } else {
            // Submit wrong answer
            handleSubmitAnswer();
          }
        }
      }, 1000);

      return () => clearInterval(timer);
    }
  }, [timeRemaining, isRevealed]);

  if (questions.length === 0) {
    return (
      <div className="p-8 flex items-center justify-center min-h-screen">
        <div style={{ color: 'var(--color-text-secondary)' }}>Loading questions...</div>
      </div>
    );
  }

  const currentQuestion = questions[currentQuestionIndex];

  const handleSelectAnswer = (index: number) => {
    if (!isRevealed) {
      setSelectedAnswer(index);
    }
  };

  const handleSubmitAnswer = () => {
    if (!isRevealed) {
      setIsRevealed(true);

      // Record answer
      const isCorrect = selectedAnswer === currentQuestion.correctAnswer;
      const timeSpent = currentQuestion.timeLimit - timeRemaining;
      const xpEarned = isCorrect ? 100 : 0;

      answerQuestion({
        questionId: currentQuestion.id,
        selectedAnswer: selectedAnswer ?? -1,
        isCorrect,
        timeSpent,
        xpEarned,
      });
    }
  };

  const submitAnswer = () => {
    if (selectedAnswer !== null) {
      handleSubmitAnswer();
    }
  };

  const handleContinue = () => {
    if (currentQuestionIndex + 1 < questions.length) {
      setCurrentQuestion(currentQuestionIndex + 1);
      setSelectedAnswer(null);
      setIsRevealed(false);
    } else {
      // Quiz complete
      const stats = completeQuiz();
      navigate(`/quiz/results/${sessionId || 'completed'}`, { state: stats });
    }
  };

  const handleAbandonQuiz = () => {
    if (confirm('Are you sure? Your progress will be lost.')) {
      navigate('/play');
    }
  };

  const isLastQuestion = currentQuestionIndex === questions.length - 1;

  return (
    <div
      className="min-h-screen p-8"
      style={{ backgroundColor: 'var(--color-bg-primary)' }}
    >
      <div className="max-w-2xl mx-auto">
        {/* Header with stats */}
        <div className="mb-8 flex items-center justify-between">
          <div>
            <h1
              className="text-3xl font-bold mb-2"
              style={{ color: 'var(--color-text-primary)' }}
            >
              {category && category.charAt(0).toUpperCase() + category.slice(1)} Quiz
            </h1>
            <p style={{ color: 'var(--color-text-secondary)' }}>
              Difficulty: {difficulty && difficulty.charAt(0).toUpperCase() + difficulty.slice(1)} • Category: {category && category.charAt(0).toUpperCase() + category.slice(1)}
            </p>
          </div>
          <div className="flex gap-6">
            <div className="text-center">
              <div className="flex items-center gap-2 mb-2">
                <BarChart3
                  size={20}
                  style={{ color: 'var(--color-brand-primary)' }}
                />
                <span
                  className="text-2xl font-bold"
                  style={{ color: 'var(--color-text-primary)' }}
                >
                  {score}
                </span>
              </div>
              <p className="text-sm" style={{ color: 'var(--color-text-secondary)' }}>
                Score
              </p>
            </div>
            <div className="text-center">
              <div className="flex items-center gap-2 mb-2">
                <Zap
                  size={20}
                  style={{ color: 'var(--color-brand-accent)' }}
                />
                <span
                  className="text-2xl font-bold"
                  style={{ color: 'var(--color-text-primary)' }}
                >
                  {xpEarned}
                </span>
              </div>
              <p className="text-sm" style={{ color: 'var(--color-text-secondary)' }}>
                XP Earned
              </p>
            </div>
          </div>
        </div>

        {/* Timer */}
        <TimerBar
          timeRemaining={timeRemaining}
          totalTime={currentQuestion.timeLimit}
        />

        {/* Question */}
        <QuestionCard
          question={currentQuestion}
          currentIndex={currentQuestionIndex}
          totalQuestions={questions.length}
        />

        {/* Answer options */}
        <div className="space-y-3 mb-8">
          {currentQuestion.options.map((option, index) => (
            <AnswerButton
              key={index}
              answer={option}
              index={index}
              isSelected={selectedAnswer === index}
              isRevealed={isRevealed}
              isCorrect={index === currentQuestion.correctAnswer}
              onClick={() => handleSelectAnswer(index)}
              disabled={isRevealed}
            />
          ))}
        </div>

        {/* Action buttons */}
        <div className="flex gap-4">
          {!isRevealed ? (
            <>
              <button
                onClick={handleSubmitAnswer}
                disabled={selectedAnswer === null}
                className="flex-1 py-3 px-6 rounded-lg font-semibold transition-all disabled:opacity-50"
                style={{
                  backgroundColor: selectedAnswer === null ? 'var(--color-bg-tertiary)' : 'var(--color-brand-primary)',
                  color: selectedAnswer === null ? 'var(--color-text-secondary)' : 'white',
                }}
              >
                Submit Answer
              </button>
              <button
                onClick={handleAbandonQuiz}
                className="px-6 py-3 rounded-lg font-semibold transition-all border-2"
                style={{
                  borderColor: 'var(--color-ui-border)',
                  color: 'var(--color-text-primary)',
                }}
              >
                Give Up
              </button>
            </>
          ) : (
            <button
              onClick={handleContinue}
              className="flex-1 py-3 px-6 rounded-lg font-semibold transition-all"
              style={{
                backgroundColor: 'var(--color-brand-primary)',
                color: 'white',
              }}
            >
              {isLastQuestion ? 'See Results' : 'Next Question'}
            </button>
          )}
        </div>

        {/* Answer feedback */}
        {isRevealed && (
          <div
            className="mt-8 p-6 rounded-lg border-l-4"
            style={{
              backgroundColor: selectedAnswer === currentQuestion.correctAnswer
                ? 'var(--color-status-success)'
                : 'var(--color-status-error)',
              borderColor: selectedAnswer === currentQuestion.correctAnswer
                ? 'var(--color-status-success)'
                : 'var(--color-status-error)',
              color: 'white',
            }}
          >
            {selectedAnswer === currentQuestion.correctAnswer ? (
              <div>
                <h3 className="font-bold mb-2">Correct! 🎉</h3>
                <p>You earned 100 XP and 100 points!</p>
              </div>
            ) : (
              <div>
                <h3 className="font-bold mb-2">Not quite right</h3>
                <p>The correct answer was: {currentQuestion.options[currentQuestion.correctAnswer]}</p>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

export default QuizSessionScreen;
