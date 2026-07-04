import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
export default function EmptyState({ title, description, icon = '📭', action, }) {
    return (_jsxs("div", { className: "flex flex-col items-center justify-center py-12 px-4", children: [_jsx("div", { className: "text-4xl mb-4", children: icon }), _jsx("h3", { className: "text-lg font-semibold text-ink-primary mb-2", children: title }), description && (_jsx("p", { className: "text-ink-secondary text-sm mb-4 text-center max-w-sm", children: description })), action && (_jsx("button", { onClick: action.onClick, className: "mt-4 px-4 py-2 bg-accent text-white rounded hover:bg-accent-dark", children: action.label }))] }));
}
export function EmptyCard({ title, description, icon = '📭', action, }) {
    return (_jsx("div", { className: "operator-card", children: _jsx(EmptyState, { title: title, description: description, icon: icon, action: action }) }));
}
