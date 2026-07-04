import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
export function SkeletonCard() {
    return (_jsx("div", { className: "operator-card animate-pulse", children: _jsxs("div", { className: "space-y-3 p-4", children: [_jsx("div", { className: "h-4 bg-panel rounded w-3/4" }), _jsx("div", { className: "h-8 bg-panel rounded w-1/2" })] }) }));
}
export function SkeletonGrid({ count = 4 }) {
    return (_jsx("div", { className: "grid grid-cols-4 gap-4", children: Array.from({ length: count }).map((_, i) => (_jsx(SkeletonCard, {}, i))) }));
}
export function SkeletonTableRow({ columns = 5 }) {
    return (_jsx("tr", { className: "border-t border-panel-border animate-pulse", children: Array.from({ length: columns }).map((_, i) => (_jsx("td", { className: "px-4 py-3", children: _jsx("div", { className: "h-4 bg-panel rounded" }) }, i))) }));
}
export function SkeletonTable({ rows = 5, columns = 5 }) {
    return (_jsx("div", { className: "operator-card", children: _jsx("div", { className: "overflow-x-auto", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { className: "bg-panel", children: _jsx("tr", { children: Array.from({ length: columns }).map((_, i) => (_jsx("th", { className: "px-4 py-2", children: _jsx("div", { className: "h-4 bg-panel-border rounded w-3/4" }) }, i))) }) }), _jsx("tbody", { children: Array.from({ length: rows }).map((_, i) => (_jsx(SkeletonTableRow, { columns: columns }, i))) })] }) }) }));
}
export function SkeletonChart({ height = 'h-48' }) {
    return (_jsx("div", { className: `operator-card animate-pulse ${height}`, children: _jsx("div", { className: "p-4 h-full bg-panel rounded" }) }));
}
export function SkeletonList({ items = 5 }) {
    return (_jsx("div", { className: "operator-card space-y-2 p-4", children: Array.from({ length: items }).map((_, i) => (_jsxs("div", { className: "p-3 border border-panel-border rounded animate-pulse", children: [_jsx("div", { className: "h-4 bg-panel rounded w-3/4 mb-2" }), _jsx("div", { className: "h-3 bg-panel rounded w-1/2" })] }, i))) }));
}
export function SkeletonHeader() {
    return (_jsxs("div", { className: "space-y-4 animate-pulse", children: [_jsx("div", { className: "h-8 bg-panel rounded w-1/3" }), _jsx("div", { className: "h-4 bg-panel rounded w-1/2" })] }));
}
export function SkeletonDetail() {
    return (_jsx("div", { className: "operator-card space-y-4 p-6 animate-pulse", children: Array.from({ length: 4 }).map((_, i) => (_jsxs("div", { className: "space-y-2", children: [_jsx("div", { className: "h-4 bg-panel rounded w-1/4" }), _jsx("div", { className: "h-5 bg-panel rounded w-1/2" })] }, i))) }));
}
