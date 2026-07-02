import { jsx as _jsx, jsxs as _jsxs, Fragment as _Fragment } from "react/jsx-runtime";
/**
 * Main app layout with sidebar and top navigation
 */
import React from 'react';
import { Outlet } from 'react-router-dom';
import { useIsAuthenticated } from '@/hooks/use-permission';
import { Sidebar } from './sidebar';
import { TopNav } from './top-nav';
import { MockBanner } from '@/components/shared/mock-banner';
export default function AppLayout() {
    const isAuthenticated = useIsAuthenticated();
    if (!isAuthenticated) {
        // Redirect to login if not authenticated
        window.location.href = '/auth/login';
        return null;
    }
    return (_jsxs(_Fragment, { children: [_jsx(MockBanner, {}), _jsxs("div", { className: "operator-shell pt-12", children: [_jsx(Sidebar, {}), _jsxs("div", { className: "operator-main flex flex-col flex-1", children: [_jsx(TopNav, {}), _jsx("main", { className: "flex-1 overflow-auto", children: _jsx(React.Suspense, { fallback: _jsx("div", { className: "p-8", children: "Loading..." }), children: _jsx(Outlet, {}) }) })] })] })] }));
}
