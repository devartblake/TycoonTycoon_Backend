/**
 * Store page - purchase items, power-ups, and cosmetics
 */

import { useEffect, useState } from 'react';
import { useProfileStore } from '@stores';
import { apiClient } from '@core/api/client';
import { ShoppingCart, AlertCircle, Clock } from 'lucide-react';
import { GridSkeleton } from '@components/skeletons/GridSkeleton';
import { EmptyState } from '@components/EmptyState';
import { PageTransition } from '@components/PageTransition';
import { useToast } from '@hooks/useToast';

interface StoreItem {
  itemId: string; // backend SKU
  name: string;
  description: string;
  icon?: string;
  category: string; // backend itemType, e.g. power-up, cosmetic, premium-subscription
  price: number;
  currencyType: 'coins' | 'diamonds';
  isRealMoney?: boolean; // no coin/diamond price — purchased with card via Stripe
  duration?: number; // For time-based items in seconds
  effect?: string;
}

type CategoryType = 'all' | 'power-up' | 'cosmetic' | 'skill-boost';

export function StorePage() {
  const profile = useProfileStore((state) => state.profile);
  const toast = useToast();
  const [items, setItems] = useState<StoreItem[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<CategoryType>('all');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [purchasing, setPurchasing] = useState<string | null>(null);

  useEffect(() => {
    const fetchStoreItems = async () => {
      try {
        setIsLoading(true);
        setError(null);
        const data = await apiClient.getStoreItems();
        setItems(data);
      } catch (err) {
        console.error('Failed to fetch store items:', err);
        const errorMsg = 'Failed to load store items. Please try again.';
        setError(errorMsg);
        toast.error(errorMsg);
      } finally {
        setIsLoading(false);
      }
    };

    fetchStoreItems();
  }, [toast]);

  const handleCardCheckout = async (item: StoreItem) => {
    if (item.category === 'premium-subscription') {
      toast.info('Subscriptions are coming soon on web — use the mobile app for now.');
      return;
    }

    try {
      setPurchasing(item.itemId);
      const session = await apiClient.createStripeCheckoutSession(item.itemId);
      // Hand off to the Stripe-hosted checkout page; fulfillment happens via webhook
      window.location.href = session.checkoutUrl;
    } catch (err: any) {
      console.error('Failed to start checkout:', err);
      const code = err?.response?.data?.error?.code;
      if (code === 'secure_session_required' || code === 'secure_session_invalid') {
        toast.error('Could not establish a secure session. Please try again.');
      } else if (code === 'STRIPE_PRICE_NOT_CONFIGURED' || code === 'STRIPE_NOT_READY') {
        toast.error('This item is not available for card purchase yet.');
      } else {
        toast.error('Failed to start checkout. Please try again.');
      }
      setPurchasing(null);
    }
  };

  const handlePurchase = async (item: StoreItem) => {
    if (!profile) {
      const msg = 'Please log in to make purchases.';
      setError(msg);
      toast.error(msg);
      return;
    }

    if (item.isRealMoney) {
      await handleCardCheckout(item);
      return;
    }

    // Check if player has enough currency
    const hasEnough =
      item.currencyType === 'coins'
        ? profile.coins >= item.price
        : profile.diamonds >= item.price;

    if (!hasEnough) {
      const msg = `Insufficient ${item.currencyType}. You need ${item.price} but only have ${item.currencyType === 'coins' ? profile.coins : profile.diamonds}.`;
      setError(msg);
      toast.error(msg);
      return;
    }

    try {
      setPurchasing(item.itemId);
      await apiClient.purchaseItem(item.itemId, item.currencyType);
      // Update profile if needed
      if (item.currencyType === 'coins') {
        useProfileStore.setState((state) => ({
          profile: state.profile ? { ...state.profile, coins: state.profile.coins - item.price } : null,
        }));
      } else {
        useProfileStore.setState((state) => ({
          profile: state.profile ? { ...state.profile, diamonds: state.profile.diamonds - item.price } : null,
        }));
      }
      setError(null);
      toast.success(`Purchased ${item.name}!`);
    } catch (err) {
      console.error('Purchase failed:', err);
      const errorMsg = 'Failed to complete purchase. Please try again.';
      setError(errorMsg);
      toast.error(errorMsg);
    } finally {
      setPurchasing(null);
    }
  };

  const getCategoryIcon = (category: string) => {
    switch (category) {
      case 'power-up':
        return '⚡';
      case 'skill-boost':
        return '🎯';
      case 'cosmetic':
        return '✨';
      default:
        return '📦';
    }
  };

  const getCategoryColor = (category: string) => {
    switch (category) {
      case 'power-up':
        return 'var(--color-brand-accent)';
      case 'skill-boost':
        return 'var(--color-status-warning)';
      case 'cosmetic':
        return 'var(--color-status-info)';
      default:
        return 'var(--color-text-secondary)';
    }
  };

  const filteredItems =
    selectedCategory === 'all'
      ? items
      : items.filter((item) => item.category === selectedCategory);

  return (
    <PageTransition>
      <div className="p-8 max-w-6xl mx-auto">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold mb-2" style={{ color: 'var(--color-text-primary)' }}>
            Store
          </h1>
          <p style={{ color: 'var(--color-text-secondary)' }}>
            Purchase items to enhance your gameplay
          </p>
        </div>

        {/* Wallet Display */}
        {profile && (
          <div className="flex gap-4">
            <div
              className="px-4 py-3 rounded-lg flex items-center gap-2"
              style={{ backgroundColor: 'var(--color-bg-secondary)' }}
            >
              <span style={{ fontSize: '1.5rem' }}>🪙</span>
              <div>
                <p style={{ color: 'var(--color-text-secondary)', fontSize: '0.875rem' }}>
                  Coins
                </p>
                <p className="font-bold" style={{ color: 'var(--color-text-primary)' }}>
                  {profile.coins.toLocaleString()}
                </p>
              </div>
            </div>
            <div
              className="px-4 py-3 rounded-lg flex items-center gap-2"
              style={{ backgroundColor: 'var(--color-bg-secondary)' }}
            >
              <span style={{ fontSize: '1.5rem' }}>💎</span>
              <div>
                <p style={{ color: 'var(--color-text-secondary)', fontSize: '0.875rem' }}>
                  Diamonds
                </p>
                <p className="font-bold" style={{ color: 'var(--color-text-primary)' }}>
                  {profile.diamonds.toLocaleString()}
                </p>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Error Alert */}
      {error && (
        <div
          className="mb-8 p-4 rounded-lg flex items-start gap-3"
          style={{ backgroundColor: 'var(--color-status-error)', color: 'white' }}
        >
          <AlertCircle size={20} className="flex-shrink-0 mt-0.5" />
          <div>
            <h3 className="font-semibold mb-1">Error</h3>
            <p className="text-sm">{error}</p>
          </div>
        </div>
      )}

      {/* Category Filter */}
      <div className="mb-8 flex gap-2 flex-wrap">
        {(['all', 'power-up', 'skill-boost', 'cosmetic'] as const).map((category) => (
          <button
            key={category}
            onClick={() => setSelectedCategory(category)}
            className="px-4 py-2 rounded-lg font-semibold text-sm transition-all"
            style={{
              backgroundColor:
                selectedCategory === category
                  ? 'var(--color-brand-primary)'
                  : 'var(--color-bg-secondary)',
              color:
                selectedCategory === category
                  ? 'white'
                  : 'var(--color-text-primary)',
              borderWidth: selectedCategory === category ? '0' : '1px',
              borderColor: 'var(--color-ui-border)',
            }}
          >
            {category.charAt(0).toUpperCase() + category.slice(1)}
          </button>
        ))}
      </div>

      {/* Items Grid */}
      {isLoading ? (
        <GridSkeleton items={6} columns={3} />
      ) : filteredItems.length === 0 ? (
        <EmptyState
          icon="📦"
          title="No Items Available"
          description={selectedCategory === 'all'
            ? 'Store is currently empty. Check back soon for new items!'
            : `No items in the ${selectedCategory} category. Try another category!`}
          action={{
            label: 'Browse All Items',
            onClick: () => setSelectedCategory('all'),
          }}
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredItems.map((item) => (
            <div
              key={item.itemId}
              className="rounded-lg overflow-hidden"
              style={{ backgroundColor: 'var(--color-bg-secondary)' }}
            >
              {/* Item Icon */}
              <div
                className="h-32 flex items-center justify-center text-5xl"
                style={{ backgroundColor: 'var(--color-bg-tertiary)' }}
              >
                {item.icon || getCategoryIcon(item.category)}
              </div>

              {/* Item Details */}
              <div className="p-4">
                <div className="flex items-start justify-between mb-2">
                  <div>
                    <h3
                      className="font-bold text-lg"
                      style={{ color: 'var(--color-text-primary)' }}
                    >
                      {item.name}
                    </h3>
                    <p
                      className="text-xs font-semibold"
                      style={{ color: getCategoryColor(item.category) }}
                    >
                      {item.category.toUpperCase()}
                    </p>
                  </div>
                </div>

                <p
                  className="text-sm mb-4"
                  style={{ color: 'var(--color-text-secondary)' }}
                >
                  {item.description}
                </p>

                {/* Duration for time-based items */}
                {item.duration && (
                  <div className="flex items-center gap-2 mb-3 text-xs">
                    <Clock size={14} style={{ color: 'var(--color-text-secondary)' }} />
                    <span style={{ color: 'var(--color-text-secondary)' }}>
                      {Math.floor(item.duration / 60)} minutes
                    </span>
                  </div>
                )}

                {/* Price and Purchase Button */}
                <div className="flex gap-2">
                  <div
                    className="flex-1 py-2 px-3 rounded-lg flex items-center justify-center gap-2 font-bold"
                    style={{
                      backgroundColor: 'var(--color-bg-tertiary)',
                      color: item.isRealMoney
                        ? 'var(--color-text-primary)'
                        : item.currencyType === 'coins' ? 'var(--color-status-warning)' : 'var(--color-status-info)',
                    }}
                  >
                    {item.isRealMoney ? (
                      <>
                        <span>💳</span> Card
                      </>
                    ) : (
                      <>
                        <span>{item.currencyType === 'coins' ? '🪙' : '💎'}</span>
                        {item.price}
                      </>
                    )}
                  </div>
                  <button
                    onClick={() => handlePurchase(item)}
                    disabled={purchasing === item.itemId}
                    className="flex-1 py-2 px-3 rounded-lg font-bold transition-all flex items-center justify-center gap-2"
                    style={{
                      backgroundColor:
                        purchasing === item.itemId
                          ? 'var(--color-text-secondary)'
                          : 'var(--color-brand-primary)',
                      color: 'white',
                      opacity: purchasing === item.itemId ? 0.6 : 1,
                      cursor: purchasing === item.itemId ? 'not-allowed' : 'pointer',
                    }}
                  >
                    <ShoppingCart size={16} />
                    {purchasing === item.itemId ? 'Buying...' : 'Buy'}
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
    </PageTransition>
  );
}

export default StorePage;
