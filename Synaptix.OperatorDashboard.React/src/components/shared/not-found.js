import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * 404 Not Found page
 */
// import React from 'react'
import { Link } from 'react-router-dom';
export default function NotFoundPage() {
    return (_jsx("div", { className: "min-h-screen bg-bg-primary flex items-center justify-center px-4", children: _jsxs("div", { className: "text-center space-y-6", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-6xl font-bold text-accent", children: "404" }), _jsx("p", { className: "text-2xl font-semibold text-ink-primary mt-2", children: "Page not found" })] }), _jsx("p", { className: "text-ink-secondary", children: "Sorry, we couldn't find the page you're looking for." }), _jsx(Link, { to: "/", className: "inline-block px-6 py-2 bg-accent text-white rounded font-medium hover:bg-accent-dark transition-smooth", children: "Go back home" })] }) }));
}
