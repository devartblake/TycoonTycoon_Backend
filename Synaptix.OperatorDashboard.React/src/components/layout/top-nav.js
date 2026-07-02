import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Top navigation bar with user profile and actions
 */
// import React from 'react'
import { useNavigate } from 'react-router-dom';
import { useAuth } from '@/hooks/use-permission';
import { useAuthStore } from '@/features/auth/store';
import { LogOut, User } from 'lucide-react';
export function TopNav() {
    const navigate = useNavigate();
    const { profile } = useAuth();
    const logout = useAuthStore((state) => state.logout);
    const handleLogout = () => {
        logout();
        navigate('/auth/login');
    };
    return (_jsx("header", { className: "border-b border-panel-border bg-panel-bg sticky top-0 z-50", children: _jsxs("div", { className: "px-6 py-4 flex items-center justify-between", children: [_jsx("div", { children: _jsx("h1", { className: "text-lg font-semibold text-ink-primary", children: "Operator Dashboard" }) }), profile && (_jsxs("div", { className: "flex items-center gap-4", children: [_jsxs("div", { className: "flex items-center gap-2 text-sm", children: [_jsx(User, { className: "w-4 h-4 text-ink-tertiary" }), _jsx("span", { className: "text-ink-secondary", children: profile.email })] }), _jsx("button", { onClick: handleLogout, className: "p-2 rounded hover:bg-bg-secondary transition-smooth text-ink-secondary hover:text-accent", title: "Logout", children: _jsx(LogOut, { className: "w-5 h-5" }) })] }))] }) }));
}
