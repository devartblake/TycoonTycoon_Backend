/**
 * Timer bar component showing remaining time for current question
 */

import { Clock } from 'lucide-react';

interface TimerBarProps {
  timeRemaining: number;
  totalTime: number;
}

export function TimerBar({ timeRemaining, totalTime }: TimerBarProps) {
  const percentage = (timeRemaining / totalTime) * 100;

  // Color based on time remaining
  let barColor = 'var(--color-status-success)';
  if (percentage < 25) {
    barColor = 'var(--color-status-error)';
  } else if (percentage < 50) {
    barColor = 'var(--color-status-warning)';
  }

  const isLowTime = timeRemaining <= 5;

  return (
    <div className="mb-6">
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center gap-2">
          <Clock
            size={18}
            style={{ color: barColor }}
            className={isLowTime ? 'animate-pulse' : ''}
          />
          <span
            className="font-semibold"
            style={{ color: barColor }}
          >
            {timeRemaining}s
          </span>
        </div>
        <span
          className="text-sm"
          style={{ color: 'var(--color-text-secondary)' }}
        >
          {totalTime}s total
        </span>
      </div>

      {/* Progress bar */}
      <div
        className="w-full h-3 rounded-full overflow-hidden"
        style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
      >
        <div
          className="h-full rounded-full transition-all duration-300"
          style={{
            width: `${percentage}%`,
            backgroundColor: barColor,
          }}
        />
      </div>

      {/* Low time warning */}
      {isLowTime && (
        <div
          className="mt-2 text-sm font-semibold text-center"
          style={{ color: 'var(--color-status-error)' }}
        >
          Hurry up! Time running out
        </div>
      )}
    </div>
  );
}

export default TimerBar;
