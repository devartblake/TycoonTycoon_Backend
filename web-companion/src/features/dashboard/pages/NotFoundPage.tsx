/**
 * 404 Not Found page
 */

import { useNavigate } from 'react-router-dom';

export function NotFoundPage() {
  const navigate = useNavigate();

  return (
    <div className="min-h-screen bg-gray-950 flex items-center justify-center px-4">
      <div className="text-center">
        <h1 className="text-6xl font-bold text-white mb-2">404</h1>
        <p className="text-2xl text-gray-400 mb-8">Page not found</p>
        <button
          onClick={() => navigate('/')}
          className="px-6 py-3 bg-primary hover:bg-secondary text-white rounded-lg transition-colors"
        >
          Back to Home
        </button>
      </div>
    </div>
  );
}

export default NotFoundPage;
