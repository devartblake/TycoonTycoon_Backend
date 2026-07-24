/**
 * PayPal approval return page.
 *
 * PayPal redirects here after the buyer approves the order, appending the order
 * id as `token`. We finalize by calling the secure capture endpoint. A 200 with
 * a non-COMPLETED status and a null transactionId means the payment isn't
 * finalized yet (the async capture webhook may still fulfill it), so we surface
 * a "pending" state rather than implying success.
 */

import { useEffect, useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { CheckCircle2, XCircle, Clock, Loader2 } from 'lucide-react';
import { apiClient } from '@core/api/client';
import { PageTransition } from '@components/PageTransition';

type Outcome = 'capturing' | 'success' | 'pending' | 'failed';

export function PayPalReturnPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  // PayPal returns the order id as `token`; accept a couple of aliases too.
  const orderId =
    searchParams.get('token') ??
    searchParams.get('orderId') ??
    searchParams.get('order_id');

  const [outcome, setOutcome] = useState<Outcome>('capturing');
  // Guard against double-invocation (React 18 StrictMode mounts effects twice).
  const startedRef = useRef(false);

  useEffect(() => {
    if (startedRef.current) return;
    startedRef.current = true;

    if (!orderId) {
      setOutcome('failed');
      return;
    }

    (async () => {
      try {
        const result = await apiClient.capturePayPalOrder(orderId);
        const status = String(result?.status ?? '').toUpperCase();
        const captured = result?.transactionId != null && status === 'COMPLETED';
        setOutcome(captured ? 'success' : 'pending');
      } catch {
        setOutcome('failed');
      }
    })();
  }, [orderId]);

  const view = {
    capturing: {
      icon: <Loader2 size={64} className="animate-spin" style={{ color: 'var(--color-brand-primary)' }} />,
      title: 'Finalizing your PayPal payment…',
      message: 'We are capturing the approved order on the server.',
    },
    success: {
      icon: <CheckCircle2 size={64} style={{ color: 'var(--color-status-success)' }} />,
      title: 'Payment Complete!',
      message: 'Thanks for your purchase — your items will appear in your account shortly.',
    },
    pending: {
      icon: <Clock size={64} style={{ color: 'var(--color-status-warning)' }} />,
      title: 'Payment pending',
      message:
        'PayPal accepted the approval, but the payment has not completed yet. Your items will appear once it is confirmed.',
    },
    failed: {
      icon: <XCircle size={64} style={{ color: 'var(--color-status-warning)' }} />,
      title: 'Capture could not finish',
      message:
        'We returned from PayPal but could not capture the order. If you were charged, it will reconcile automatically.',
    },
  }[outcome];

  return (
    <PageTransition>
      <div className="p-8 max-w-2xl mx-auto">
        <div
          className="p-8 rounded-lg text-center"
          style={{ backgroundColor: 'var(--color-bg-secondary)' }}
        >
          <div className="flex justify-center mb-4">{view.icon}</div>
          <h1 className="text-2xl font-bold mb-2" style={{ color: 'var(--color-text-primary)' }}>
            {view.title}
          </h1>
          <p className="mb-6" style={{ color: 'var(--color-text-secondary)' }}>
            {view.message}
          </p>
          {outcome !== 'capturing' && (
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
          )}
        </div>
      </div>
    </PageTransition>
  );
}

export default PayPalReturnPage;
