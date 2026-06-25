/**
 * Toast container - renders all notifications
 */

import { useNotificationStore } from '@stores/notificationStore';
import Toast from './Toast';

export function ToastContainer() {
  const notifications = useNotificationStore((state) => state.notifications);
  const remove = useNotificationStore((state) => state.remove);

  if (notifications.length === 0) {
    return null;
  }

  return (
    <div
      className="fixed bottom-4 right-4 z-50 space-y-2 pointer-events-none"
      style={{ maxWidth: '400px' }}
    >
      {notifications.map((notification) => (
        <div key={notification.id} className="pointer-events-auto">
          <Toast
            id={notification.id}
            message={notification.message}
            type={notification.type}
            onClose={remove}
          />
        </div>
      ))}
    </div>
  );
}

export default ToastContainer;
