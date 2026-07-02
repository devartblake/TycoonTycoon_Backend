import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Authentication layout (login, forgot password, reset password)
 */
import React from 'react';
import { Outlet } from 'react-router-dom';
export default function AuthLayout() {
    return (_jsx("div", { className: "min-h-screen bg-bg-primary flex items-center justify-center py-12 px-4", children: _jsxs("div", { className: "w-full max-w-md space-y-8", children: [_jsxs("div", { className: "text-center", children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Synaptix" }), _jsx("p", { className: "mt-2 text-sm text-ink-tertiary", children: "Operator Dashboard" })] }), _jsx("div", { className: "operator-card", children: _jsx(React.Suspense, { fallback: _jsx("div", { className: "text-center py-8", children: "Loading..." }), children: _jsx(Outlet, {}) }) }), _jsx("div", { className: "text-center text-xs text-ink-tertiary", children: _jsx("p", { children: "\u00A9 2026 Synaptix. All rights reserved." }) })] }) }));
}
