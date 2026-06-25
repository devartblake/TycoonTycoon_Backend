/**
 * Individual toast notification component
 */

import { CheckCircle, AlertCircle, Info, AlertTriangle, X } from 'lucide-react';
import type { NotificationType } from '@stores/notificationStore';

interface ToastProps {
  id: string;
  message: string;
  type: NotificationType;
  onClose: (id: string) => void;
}

export function Toast({ id, message, type, onClose }: ToastProps) {
  const getStyles = () => {
    switch (type) {
      case 'success':
        return {
          bg: 'var(--color-status-success)',
          icon: CheckCircle,
        };
      case 'error':
        return {
          bg: 'var(--color-status-error)',
          icon: AlertCircle,
        };
      case 'warning':
        return {
          bg: 'var(--color-status-warning)',
          icon: AlertTriangle,
        };
      case 'info':
      default:
        return {
          bg: 'var(--color-status-info)',
          icon: Info,
        };
    }
  };

  const { bg, icon: Icon } = getStyles();

  return (
    <div
      className="flex items-center gap-3 px-4 py-3 rounded-lg shadow-lg animate-pulse-once"
      style={{ backgroundColor: bg, color: 'white' }}
    >
      <Icon size={20} className="flex-shrink-0" />
      <p className="flex-1 text-sm font-medium">{message}</p>
      <button
        onClick={() => onClose(id)}
        className="flex-shrink-0 p-1 hover:bg-white/20 rounded transition-colors"
        aria-label="Close notification"
      >
        <X size={16} />
      </button>
    </div>
  );
}

export default Toast;
