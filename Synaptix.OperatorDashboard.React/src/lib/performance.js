import { startSpan } from './sentry';
/**
 * Performance monitoring and optimization utilities
 */
// Memoize expensive computations
export function memoize(fn) {
    const cache = new Map();
    return ((...args) => {
        const key = JSON.stringify(args);
        if (cache.has(key)) {
            return cache.get(key);
        }
        const result = fn(...args);
        cache.set(key, result);
        return result;
    });
}
// Debounce function calls
export function debounce(fn, delay) {
    let timeoutId;
    return ((...args) => {
        clearTimeout(timeoutId);
        timeoutId = setTimeout(() => fn(...args), delay);
    });
}
// Throttle function calls
export function throttle(fn, limit) {
    let lastRun = 0;
    return ((...args) => {
        const now = Date.now();
        if (now - lastRun >= limit) {
            lastRun = now;
            fn(...args);
        }
    });
}
// Performance measurement
export function measurePerformance(label, fn) {
    const span = startSpan(label);
    const startTime = performance.now();
    fn();
    const duration = performance.now() - startTime;
    span.end();
    if (duration > 100) {
        console.warn(`⚠️ Slow operation "${label}": ${duration.toFixed(2)}ms`);
    }
    return duration;
}
// Batch DOM updates
export function batchDOMUpdates(updates) {
    requestAnimationFrame(() => {
        updates.forEach((update) => update());
    });
}
// Intersection Observer for lazy loading
export function useIntersectionObserver(ref, options = {}) {
    const [isVisible, setIsVisible] = React.useState(false);
    React.useEffect(() => {
        if (!ref.current)
            return;
        const observer = new IntersectionObserver(([entry]) => {
            if (entry.isIntersecting) {
                setIsVisible(true);
                observer.disconnect();
            }
        }, { threshold: 0.1, ...options });
        observer.observe(ref.current);
        return () => observer.disconnect();
    }, [ref, options]);
    return [isVisible];
}
// Virtual scroll optimization hint
export function useVirtualScroll(itemCount, itemHeight) {
    const containerRef = React.useRef(null);
    const [visibleRange, setVisibleRange] = React.useState({ start: 0, end: 20 });
    React.useEffect(() => {
        const container = containerRef.current;
        if (!container)
            return;
        const handleScroll = throttle(() => {
            const scrollTop = container.scrollTop;
            const visibleHeight = container.clientHeight;
            const start = Math.floor(scrollTop / itemHeight);
            const end = start + Math.ceil(visibleHeight / itemHeight) + 2;
            setVisibleRange({
                start: Math.max(0, start),
                end: Math.min(itemCount, end),
            });
        }, 16);
        container.addEventListener('scroll', handleScroll);
        return () => container.removeEventListener('scroll', handleScroll);
    }, [itemCount, itemHeight]);
    return { containerRef, visibleRange };
}
// Preload images
export function preloadImage(src) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.onload = () => resolve();
        img.onerror = reject;
        img.src = src;
    });
}
// Prefetch resource
export function prefetchResource(url, as = 'script') {
    const link = document.createElement('link');
    link.rel = 'prefetch';
    link.as = as;
    link.href = url;
    document.head.appendChild(link);
}
// Bundle analyzer hints
export function logBundleHints() {
    if (import.meta.env.DEV) {
        console.log('📦 Bundle optimization tips:');
        console.log('- Use React.lazy() for code-splitting');
        console.log('- Memoize expensive components with React.memo()');
        console.log('- Use useCallback/useMemo for expensive computations');
        console.log('- Lazy-load images with loading="lazy"');
        console.log('- Prefetch critical resources with prefetchResource()');
    }
}
// Export React for hook usage
import * as React from 'react';
