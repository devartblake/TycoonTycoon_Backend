/**
 * Mock question data for testing
 * In production, questions will come from the backend API
 */

import type { Question } from '@stores/quizSessionStore';

export const mockQuestions: Record<string, Question[]> = {
  science: [
    {
      id: 'q1',
      question: 'What is the chemical symbol for gold?',
      category: 'science',
      difficulty: 'easy',
      options: ['Au', 'Ag', 'Gd', 'Go'],
      correctAnswer: 0,
      timeLimit: 30,
    },
    {
      id: 'q2',
      question: 'What is the speed of light in vacuum?',
      category: 'science',
      difficulty: 'medium',
      options: ['3 × 10⁸ m/s', '3 × 10⁹ m/s', '3 × 10⁷ m/s', '3 × 10¹⁰ m/s'],
      correctAnswer: 0,
      timeLimit: 30,
    },
    {
      id: 'q3',
      question: 'Which organelle is responsible for producing energy in a cell?',
      category: 'science',
      difficulty: 'easy',
      options: ['Nucleus', 'Mitochondria', 'Chloroplast', 'Ribosome'],
      correctAnswer: 1,
      timeLimit: 30,
    },
    {
      id: 'q4',
      question: 'What is the most abundant gas in Earth\'s atmosphere?',
      category: 'science',
      difficulty: 'easy',
      options: ['Oxygen', 'Carbon Dioxide', 'Nitrogen', 'Argon'],
      correctAnswer: 2,
      timeLimit: 30,
    },
    {
      id: 'q5',
      question: 'What is the atomic number of Hydrogen?',
      category: 'science',
      difficulty: 'easy',
      options: ['0', '1', '2', '3'],
      correctAnswer: 1,
      timeLimit: 30,
    },
  ],
  history: [
    {
      id: 'h1',
      question: 'In what year did World War II end?',
      category: 'history',
      difficulty: 'easy',
      options: ['1943', '1944', '1945', '1946'],
      correctAnswer: 2,
      timeLimit: 30,
    },
    {
      id: 'h2',
      question: 'Who was the first President of the United States?',
      category: 'history',
      difficulty: 'easy',
      options: [
        'Thomas Jefferson',
        'George Washington',
        'John Adams',
        'Benjamin Franklin',
      ],
      correctAnswer: 1,
      timeLimit: 30,
    },
    {
      id: 'h3',
      question: 'The Renaissance began in which country?',
      category: 'history',
      difficulty: 'medium',
      options: ['France', 'Italy', 'Spain', 'Germany'],
      correctAnswer: 1,
      timeLimit: 30,
    },
    {
      id: 'h4',
      question: 'Which ancient wonder is still partially standing today?',
      category: 'history',
      difficulty: 'medium',
      options: [
        'Hanging Gardens',
        'Colossus of Rhodes',
        'Great Pyramid of Giza',
        'Lighthouse of Alexandria',
      ],
      correctAnswer: 2,
      timeLimit: 30,
    },
    {
      id: 'h5',
      question: 'What year did the Titanic sink?',
      category: 'history',
      difficulty: 'easy',
      options: ['1910', '1911', '1912', '1913'],
      correctAnswer: 2,
      timeLimit: 30,
    },
  ],
  geography: [
    {
      id: 'g1',
      question: 'What is the capital of France?',
      category: 'geography',
      difficulty: 'easy',
      options: ['Lyon', 'Paris', 'Marseille', 'Nice'],
      correctAnswer: 1,
      timeLimit: 30,
    },
    {
      id: 'g2',
      question: 'Which is the largest continent by area?',
      category: 'geography',
      difficulty: 'easy',
      options: ['Africa', 'Asia', 'North America', 'Europe'],
      correctAnswer: 1,
      timeLimit: 30,
    },
    {
      id: 'g3',
      question: 'What is the longest river in the world?',
      category: 'geography',
      difficulty: 'easy',
      options: ['Amazon', 'Nile', 'Yangtze', 'Mississippi'],
      correctAnswer: 1,
      timeLimit: 30,
    },
    {
      id: 'g4',
      question: 'How many countries are in the European Union?',
      category: 'geography',
      difficulty: 'medium',
      options: ['25', '27', '30', '32'],
      correctAnswer: 1,
      timeLimit: 30,
    },
    {
      id: 'g5',
      question: 'What is the smallest country in the world by area?',
      category: 'geography',
      difficulty: 'medium',
      options: ['Monaco', 'Liechtenstein', 'Vatican City', 'San Marino'],
      correctAnswer: 2,
      timeLimit: 30,
    },
  ],
  literature: [
    {
      id: 'l1',
      question: 'Who wrote "Romeo and Juliet"?',
      category: 'literature',
      difficulty: 'easy',
      options: [
        'Jane Austen',
        'William Shakespeare',
        'Charles Dickens',
        'Mark Twain',
      ],
      correctAnswer: 1,
      timeLimit: 30,
    },
    {
      id: 'l2',
      question: 'What is the first book in the "Harry Potter" series?',
      category: 'literature',
      difficulty: 'easy',
      options: [
        'Chamber of Secrets',
        'Philosopher\'s Stone',
        'Prisoner of Azkaban',
        'Goblet of Fire',
      ],
      correctAnswer: 1,
      timeLimit: 30,
    },
    {
      id: 'l3',
      question: 'Who is the author of "1984"?',
      category: 'literature',
      difficulty: 'easy',
      options: ['George Orwell', 'Aldous Huxley', 'Ray Bradbury', 'Isaac Asimov'],
      correctAnswer: 0,
      timeLimit: 30,
    },
    {
      id: 'l4',
      question: 'What is the real name of "Mark Twain"?',
      category: 'literature',
      difficulty: 'medium',
      options: [
        'Samuel Clemens',
        'Walter Scott',
        'Herman Melville',
        'Nathaniel Hawthorne',
      ],
      correctAnswer: 0,
      timeLimit: 30,
    },
    {
      id: 'l5',
      question: 'How many plays did Shakespeare write?',
      category: 'literature',
      difficulty: 'hard',
      options: ['32', '37', '42', '47'],
      correctAnswer: 1,
      timeLimit: 30,
    },
  ],
};

export function getQuestionsByCategory(
  category: string,
  count: number = 5
): Question[] {
  const categoryQuestions = mockQuestions[category] || mockQuestions.science;
  return categoryQuestions.slice(0, count);
}

export function getAllCategories(): string[] {
  return Object.keys(mockQuestions);
}

export function getQuestionsByDifficulty(
  difficulty: 'easy' | 'medium' | 'hard',
  count: number = 5
): Question[] {
  const allQuestions = Object.values(mockQuestions).flat();
  return allQuestions
    .filter((q) => q.difficulty === difficulty)
    .slice(0, count);
}
