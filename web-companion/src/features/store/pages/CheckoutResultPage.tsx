/**
 * Stripe checkout return page (success / cancelled)
 * Fulfillment is applied server-side by the Stripe webhook, so this page only
 * confirms the redirect outcome — it does not grant anything itself.
 */

import { useNavigate, useSearchParams } from 'react-router-dom';
import { CheckCircle2, XCircle } from 'lucide-react';
import { PageTransition } from '@components/PageTransition';

export function CheckoutResultPage({ status }: { status: 'success' | 'cancelled' }) {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const sessionId = searchParams.get('session_id');
  const isSuccess = status === 'success';

  return (
    <PageTransition>
      <div className="p-8 max-w-2xl mx-auto">
        <div
          className="p-8 rounded-lg text-center"
          style={{ backgroundColor: 'var(--color-bg-secondary)' }}
        >
          <div className="flex justify-center mb-4">
            {isSuccess ? (
              <CheckCircle2 size={64} style={{ color: 'var(--color-status-success)' }} />
            ) : (
              <XCircle size={64} style={{ color: 'var(--color-status-warning)' }} />
            )}
          </div>
          <h1
            className="text-2xl font-bold mb-2"
            style={{ color: 'var(--color-text-primary)' }}
          >
            {isSuccess ? 'Payment Complete!' : 'Checkout Cancelled'}
          </h1>
          <p className="mb-6" style={{ color: 'var(--color-text-secondary)' }}>
            {isSuccess
              ? 'Thanks for your purchase — your items will appear in your account shortly.'
              : 'No charge was made. You can pick up where you left off any time.'}
          </p>
          {isSuccess && sessionId && (
            <p
              className="text-xs mb-6 break-all"
              style={{ color: 'var(--color-text-secondary)' }}
            >
              Reference: {sessionId}
            </p>
          )}
          <div className="flex gap-3 justify-center">
            <button
              onClick={() => navigate('/store')}
              className="px-6 py-2 rounded-lg font-semibold"
              style={{ backgroundColor: 'var(--color-brand-primary)', color: 'white' }}
            >
              Back to Store
            </button>
            <button
              onClick={() => navigate('/')}
              className="px-6 py-2 rounded-lg font-semibold border"
              style={{
                borderColor: 'var(--color-ui-border)',
                color: 'var(--color-text-primary)',
              }}
            >
              Go to Dashboard
            </button>
          </div>
        </div>
      </div>
    </PageTransition>
  );
}

export default CheckoutResultPage;
