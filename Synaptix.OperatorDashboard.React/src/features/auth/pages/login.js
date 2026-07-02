import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Login page
 */
import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuthStore } from '@/features/auth/store';
import { adminLogin } from '@/features/auth/api';
import { getMockMode, setMockMode } from '@/lib/api-config';
const loginSchema = z.object({
    email: z.string().email('Invalid email address'),
    password: z.string().min(6, 'Password must be at least 6 characters'),
});
export default function LoginPage() {
    const navigate = useNavigate();
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState(null);
    const setTokens = useAuthStore((state) => state.setTokens);
    const setProfile = useAuthStore((state) => state.setProfile);
    const { register, handleSubmit, formState: { errors }, } = useForm({
        resolver: zodResolver(loginSchema),
    });
    const onSubmit = async (data) => {
        setIsLoading(true);
        setError(null);
        try {
            // Handle mock mode - allow any email/password
            if (getMockMode()) {
                setTokens('mock-token-' + Date.now(), 'mock-refresh-' + Date.now(), 3600);
                setProfile({
                    email: data.email,
                    permissions: [
                        'users:read',
                        'users:write',
                        'notifications:read',
                        'notifications:write',
                        'anti-cheat:read',
                        'anti-cheat:write',
                        'audit:read',
                        'events:read',
                        'storage:read',
                        'config:read',
                        'economy:read',
                        'content:read',
                        'operations:read',
                        'personalization:read',
                    ],
                });
                navigate('/dashboard');
                return;
            }
            // Call login endpoint
            const response = await adminLogin(data.email, data.password);
            // Store tokens and profile
            setTokens(response.accessToken, response.refreshToken, response.expiresIn);
            setProfile(response.admin);
            // Redirect to dashboard
            navigate('/dashboard');
        }
        catch (err) {
            const errorMessage = err instanceof Error ? err.message : 'Login failed. Please try again.';
            setError(errorMessage);
        }
        finally {
            setIsLoading(false);
        }
    };
    return (_jsxs("form", { onSubmit: handleSubmit(onSubmit), className: "space-y-4", children: [_jsx("h2", { className: "text-2xl font-bold text-center text-ink-primary mb-6", children: "Sign in to your account" }), error && (_jsx("div", { className: "p-4 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm", children: error })), _jsxs("div", { children: [_jsx("label", { htmlFor: "email", className: "block text-sm font-medium text-ink-primary mb-1", children: "Email address" }), _jsx("input", { ...register('email'), id: "email", type: "email", className: "w-full px-3 py-2 border border-panel-border rounded focus-ring", placeholder: "admin@synaptix.com" }), errors.email && (_jsx("p", { className: "text-xs text-status-offline mt-1", children: errors.email.message }))] }), _jsxs("div", { children: [_jsx("label", { htmlFor: "password", className: "block text-sm font-medium text-ink-primary mb-1", children: "Password" }), _jsx("input", { ...register('password'), id: "password", type: "password", className: "w-full px-3 py-2 border border-panel-border rounded focus-ring", placeholder: "\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022" }), errors.password && (_jsx("p", { className: "text-xs text-status-offline mt-1", children: errors.password.message }))] }), _jsx("button", { type: "submit", disabled: isLoading, className: "w-full px-4 py-2 bg-accent text-white rounded font-medium hover:bg-accent-dark transition-smooth disabled:opacity-50", children: isLoading ? 'Signing in...' : 'Sign in' }), !getMockMode() && (_jsxs("div", { className: "p-3 bg-blue-50 border border-blue-200 rounded text-sm text-blue-900", children: [_jsxs("p", { className: "mb-2", children: ["\uD83C\uDFAD ", _jsx("strong", { children: "No backend?" }), " Enable mock mode to test the UI with simulated data."] }), _jsx("button", { type: "button", onClick: () => {
                            setMockMode(true);
                            window.location.reload();
                        }, className: "text-blue-700 hover:underline font-medium text-xs", children: "Enable Mock Mode" })] })), getMockMode() && (_jsx("div", { className: "p-3 bg-yellow-50 border border-yellow-200 rounded text-sm text-yellow-900", children: _jsxs("p", { children: ["\uD83C\uDFAD ", _jsx("strong", { children: "Mock mode enabled" }), " \u2014 Enter any email and password to proceed"] }) })), _jsx("div", { className: "text-center", children: _jsx(Link, { to: "/auth/forgot-password", className: "text-sm text-accent hover:underline", children: "Forgot your password?" }) })] }));
}
