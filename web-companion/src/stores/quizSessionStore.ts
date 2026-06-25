/**
 * Quiz session state store - manages active quiz game state
 */

import { create } from 'zustand';

export interface Question {
  id: string;
  question: string;
  category: string;
  difficulty: 'easy' | 'medium' | 'hard';
  options: string[];
  correctAnswer: number; // Index of correct option
  timeLimit: number; // Seconds
}

export interface QuizAnswer {
  questionId: string;
  selectedAnswer: number;
  isCorrect: boolean;
  timeSpent: number; // Seconds
  xpEarned: number;
}

export interface QuizSessionState {
  // Session info
  sessionId: string | null;
  category: string | null;
  difficulty: 'easy' | 'medium' | 'hard' | null;
  totalQuestions: number;

  // Current progress
  currentQuestionIndex: number;
  questions: Question[];
  answers: QuizAnswer[];

  // Game state
  isActive: boolean;
  isPaused: boolean;
  timeRemaining: number; // For current question
  totalTimeElapsed: number;

  // Score tracking
  score: number;
  streak: number;
  xpEarned: number;
  coinsEarned: number;

  // Power-ups and skills active
  activeSkills: string[];
  activePowerUps: Array<{ id: string; name: string; expiresAt: number }>;

  // Multipliers
  xpMultiplier: number; // From skills/streaks/bonuses
  coinMultiplier: number;

  // Actions
  startQuiz: (
    questions: Question[],
    category: string,
    difficulty: 'easy' | 'medium' | 'hard'
  ) => void;
  setCurrentQuestion: (index: number) => void;
  answerQuestion: (answer: QuizAnswer) => void;
  setTimeRemaining: (time: number) => void;
  updateTotalTime: (elapsed: number) => void;
  addStreak: () => void;
  resetStreak: () => void;
  applyXpMultiplier: (multiplier: number) => void;
  applyCoinMultiplier: (multiplier: number) => void;
  activateSkill: (skillId: string) => void;
  deactivateSkill: (skillId: string) => void;
  activatePowerUp: (id: string, name: string, durationMs: number) => void;
  completeQuiz: () => QuizSessionStats;
  pauseQuiz: () => void;
  resumeQuiz: () => void;
  abandonQuiz: () => void;
}

export interface QuizSessionStats {
  totalQuestions: number;
  correctAnswers: number;
  accuracy: number; // Percentage
  totalScore: number;
  totalXp: number;
  totalCoins: number;
  finalStreak: number;
  timeSpent: number;
  categoryStats: {
    category: string;
    correct: number;
    total: number;
    accuracy: number;
  };
}

const generateSessionId = () => `quiz_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

export const useQuizSessionStore = create<QuizSessionState>((set, get) => ({
  sessionId: null,
  category: null,
  difficulty: null,
  totalQuestions: 0,

  currentQuestionIndex: 0,
  questions: [],
  answers: [],

  isActive: false,
  isPaused: false,
  timeRemaining: 0,
  totalTimeElapsed: 0,

  score: 0,
  streak: 0,
  xpEarned: 0,
  coinsEarned: 0,

  activeSkills: [],
  activePowerUps: [],

  xpMultiplier: 1,
  coinMultiplier: 1,

  startQuiz: (questions, category, difficulty) =>
    set({
      sessionId: generateSessionId(),
      questions,
      category,
      difficulty,
      totalQuestions: questions.length,
      isActive: true,
      isPaused: false,
      currentQuestionIndex: 0,
      answers: [],
      score: 0,
      streak: 0,
      xpEarned: 0,
      coinsEarned: 0,
      xpMultiplier: 1,
      coinMultiplier: 1,
      activeSkills: [],
      activePowerUps: [],
      timeRemaining: questions[0]?.timeLimit || 30,
      totalTimeElapsed: 0,
    }),

  setCurrentQuestion: (index) => {
    const state = get();
    if (index < state.questions.length) {
      set({
        currentQuestionIndex: index,
        timeRemaining: state.questions[index].timeLimit,
      });
    }
  },

  answerQuestion: (answer) =>
    set((state) => {
      const newAnswers = [...state.answers, answer];
      const correctCount = newAnswers.filter((a) => a.isCorrect).length;
      const newScore = correctCount * 100; // Base: 100 points per correct answer

      return {
        answers: newAnswers,
        score: newScore,
        xpEarned: answer.xpEarned,
      };
    }),

  setTimeRemaining: (time) => set({ timeRemaining: time }),

  updateTotalTime: (elapsed) => set({ totalTimeElapsed: elapsed }),

  addStreak: () =>
    set((state) => ({
      streak: state.streak + 1,
    })),

  resetStreak: () => set({ streak: 0 }),

  applyXpMultiplier: (multiplier) =>
    set((state) => ({
      xpMultiplier: state.xpMultiplier * multiplier,
    })),

  applyCoinMultiplier: (multiplier) =>
    set((state) => ({
      coinMultiplier: state.coinMultiplier * multiplier,
    })),

  activateSkill: (skillId) =>
    set((state) => ({
      activeSkills: [...new Set([...state.activeSkills, skillId])],
    })),

  deactivateSkill: (skillId) =>
    set((state) => ({
      activeSkills: state.activeSkills.filter((s) => s !== skillId),
    })),

  activatePowerUp: (id, name, durationMs) =>
    set((state) => ({
      activePowerUps: [
        ...state.activePowerUps,
        { id, name, expiresAt: Date.now() + durationMs },
      ],
    })),

  completeQuiz: () => {
    const state = get();
    const correctAnswers = state.answers.filter((a) => a.isCorrect).length;
    const accuracy = (correctAnswers / state.totalQuestions) * 100;

    // Calculate bonuses
    let xpBonus = state.xpEarned;
    let coinBonus = Math.floor(state.score / 10);

    // Apply multipliers
    xpBonus = Math.floor(xpBonus * state.xpMultiplier);
    coinBonus = Math.floor(coinBonus * state.coinMultiplier);

    // Streak bonus
    if (correctAnswers === state.totalQuestions) {
      xpBonus = Math.floor(xpBonus * 1.5); // 50% bonus for perfect score
    } else if (correctAnswers >= state.totalQuestions * 0.8) {
      xpBonus = Math.floor(xpBonus * 1.25); // 25% bonus for 80%+
    }

    const stats: QuizSessionStats = {
      totalQuestions: state.totalQuestions,
      correctAnswers,
      accuracy,
      totalScore: state.score,
      totalXp: xpBonus,
      totalCoins: coinBonus,
      finalStreak: state.streak,
      timeSpent: state.totalTimeElapsed,
      categoryStats: {
        category: state.category || 'Unknown',
        correct: correctAnswers,
        total: state.totalQuestions,
        accuracy,
      },
    };

    return stats;
  },

  pauseQuiz: () => set({ isPaused: true }),

  resumeQuiz: () => set({ isPaused: false }),

  abandonQuiz: () =>
    set({
      isActive: false,
      isPaused: false,
      sessionId: null,
      answers: [],
      currentQuestionIndex: 0,
    }),
}));

export default useQuizSessionStore;
