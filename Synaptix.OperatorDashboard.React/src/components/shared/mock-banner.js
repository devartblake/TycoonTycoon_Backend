import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Mock mode banner - displays when using mock API
 */
import { showMockBanner, setMockMode } from '@/lib/api-config';
export function MockBanner() {
    if (!showMockBanner()) {
        return null;
    }
    return (_jsxs("div", { className: "fixed top-0 left-0 right-0 z-40 bg-yellow-500/20 border-b border-yellow-500/50 px-4 py-2 flex items-center justify-between", children: [_jsxs("div", { className: "text-sm font-medium text-yellow-900", children: ["\uD83C\uDFAD ", _jsx("strong", { children: "MOCK API MODE" }), " \u2014 Using simulated data (no backend connection required)"] }), _jsx("button", { onClick: () => setMockMode(false), className: "text-xs px-2 py-1 hover:bg-yellow-500/30 rounded transition-colors", title: "Switch to real API", children: "\u2715 Disable Mock Mode" })] }));
}
