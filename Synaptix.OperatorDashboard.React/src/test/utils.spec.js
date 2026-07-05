import { describe, it, expect, vi } from 'vitest';
import { debounce, throttle, memoize } from '@/lib/performance';
describe('Performance Utilities', () => {
    describe('debounce', () => {
        it('should delay function execution', async () => {
            const fn = vi.fn();
            const debounced = debounce(fn, 100);
            debounced();
            debounced();
            debounced();
            expect(fn).not.toHaveBeenCalled();
            await new Promise(resolve => setTimeout(resolve, 150));
            expect(fn).toHaveBeenCalledOnce();
        });
        it('should call with last arguments', async () => {
            const fn = vi.fn();
            const debounced = debounce(fn, 100);
            debounced(1);
            debounced(2);
            debounced(3);
            await new Promise(resolve => setTimeout(resolve, 150));
            expect(fn).toHaveBeenCalledWith(3);
        });
    });
    describe('throttle', () => {
        it('should limit function calls', () => {
            const fn = vi.fn();
            const throttled = throttle(fn, 100);
            throttled();
            throttled();
            throttled();
            expect(fn).toHaveBeenCalledOnce();
        });
        it('should call after time limit', async () => {
            const fn = vi.fn();
            const throttled = throttle(fn, 100);
            throttled();
            await new Promise(resolve => setTimeout(resolve, 150));
            throttled();
            expect(fn).toHaveBeenCalledTimes(2);
        });
    });
    describe('memoize', () => {
        it('should cache results', () => {
            const fn = vi.fn((x) => x * 2);
            const memoized = memoize(fn);
            expect(memoized(5)).toBe(10);
            expect(memoized(5)).toBe(10);
            expect(fn).toHaveBeenCalledOnce();
        });
        it('should handle different arguments', () => {
            const fn = vi.fn((x) => x * 2);
            const memoized = memoize(fn);
            memoized(5);
            memoized(10);
            expect(fn).toHaveBeenCalledTimes(2);
        });
    });
});
