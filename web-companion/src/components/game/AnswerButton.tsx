/**
 * Answer button component with selection and reveal animation
 */

import { Check, X } from 'lucide-react';

interface AnswerButtonProps {
  answer: string;
  index: number;
  isSelected: boolean;
  isRevealed: boolean;
  isCorrect: boolean;
  onClick: () => void;
  disabled?: boolean;
}

export function AnswerButton({
  answer,
  index,
  isSelected,
  isRevealed,
  isCorrect,
  onClick,
  disabled = false,
}: AnswerButtonProps) {
  let backgroundColor = 'var(--color-bg-secondary)';
  let borderColor = 'var(--color-ui-border)';
  let textColor = 'var(--color-text-primary)';

  // Determine styling based on state
  if (isRevealed) {
    if (isCorrect) {
      backgroundColor = 'var(--color-status-success)';
      borderColor = 'var(--color-status-success)';
      textColor = 'white';
    } else if (isSelected && !isCorrect) {
      backgroundColor = 'var(--color-status-error)';
      borderColor = 'var(--color-status-error)';
      textColor = 'white';
    } else {
      backgroundColor = 'var(--color-bg-tertiary)';
      borderColor = 'var(--color-ui-border)';
      textColor = 'var(--color-text-secondary)';
    }
  } else if (isSelected) {
    backgroundColor = 'var(--color-brand-primary)';
    borderColor = 'var(--color-brand-primary)';
    textColor = 'white';
  }

  const optionLabel = String.fromCharCode(65 + index); // A, B, C, D

  return (
    <button
      onClick={onClick}
      disabled={disabled || isRevealed}
      className="w-full p-4 rounded-lg border-2 transition-all duration-200 flex items-start gap-4 text-left hover:scale-102 disabled:cursor-not-allowed"
      style={{
        backgroundColor,
        borderColor,
        transform: isSelected && !isRevealed ? 'scale(1.02)' : 'scale(1)',
      }}
    >
      {/* Option label */}
      <div
        className="flex-shrink-0 w-8 h-8 rounded-lg flex items-center justify-center font-bold text-sm"
        style={{
          backgroundColor: isSelected ? 'rgba(255, 255, 255, 0.2)' : 'var(--color-bg-tertiary)',
          color: textColor,
        }}
      >
        {optionLabel}
      </div>

      {/* Answer text */}
      <span
        className="flex-1 font-medium"
        style={{ color: textColor }}
      >
        {answer}
      </span>

      {/* Reveal icon */}
      {isRevealed && (
        <div className="flex-shrink-0">
          {isCorrect ? (
            <Check size={20} style={{ color: 'white' }} />
          ) : isSelected ? (
            <X size={20} style={{ color: 'white' }} />
          ) : null}
        </div>
      )}
    </button>
  );
}

export default AnswerButton;
