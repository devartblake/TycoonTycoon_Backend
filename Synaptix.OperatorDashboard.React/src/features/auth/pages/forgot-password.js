import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Forgot password page
 */
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import ErrorBoundary from '@/components/shared/error-boundary';
import { adminForgotPassword } from '@/features/auth/api';
const forgotPasswordSchema = z.object({
    email: z.string().email('Invalid email address'),
});
export default function ForgotPasswordPage() {
    const [isLoading, setIsLoading] = useState(false);
    const [isSubmitted, setIsSubmitted] = useState(false);
    const [error, setError] = useState(null);
    const { register, handleSubmit, formState: { errors }, watch, } = useForm({
        resolver: zodResolver(forgotPasswordSchema),
    });
    const email = watch('email');
    const onSubmit = async (data) => {
        setIsLoading(true);
        setError(null);
        try {
            await adminForgotPassword(data.email);
            setIsSubmitted(true);
        }
        catch (err) {
            const errorMessage = err instanceof Error ? err.message : 'Failed to send reset email.';
            setError(errorMessage);
        }
        finally {
            setIsLoading(false);
        }
    };
    if (isSubmitted) {
        return (_jsx(ErrorBoundary, { children: _jsxs("div", { className: "space-y-4", children: [_jsx("h2", { className: "text-2xl font-bold text-center text-ink-primary", children: "Check your email" }), _jsxs("p", { className: "text-center text-ink-secondary", children: ["We've sent a password reset link to ", _jsx("strong", { children: email }), ". Click the link in the email to reset your password."] }), _jsx("p", { className: "text-center text-sm text-ink-tertiary", children: "The link will expire in 15 minutes." }), _jsx("div", { className: "text-center pt-4", children: _jsx(Link, { to: "/auth/login", className: "text-sm text-accent hover:underline", children: "Back to login" }) })] }) }));
    }
    return (_jsx(ErrorBoundary, { children: _jsxs("form", { onSubmit: handleSubmit(onSubmit), className: "space-y-4", children: [_jsx("h2", { className: "text-2xl font-bold text-center text-ink-primary mb-6", children: "Reset your password" }), error && (_jsx("div", { className: "p-4 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm", children: error })), _jsx("p", { className: "text-sm text-ink-secondary text-center", children: "Enter your email address and we'll send you a link to reset your password." }), _jsxs("div", { children: [_jsx("label", { htmlFor: "email", className: "block text-sm font-medium text-ink-primary mb-1", children: "Email address" }), _jsx("input", { ...register('email'), id: "email", type: "email", className: "w-full px-3 py-2 border border-panel-border rounded focus-ring", placeholder: "admin@synaptix.com" }), errors.email && (_jsx("p", { className: "text-xs text-status-offline mt-1", children: errors.email.message }))] }), _jsx("button", { type: "submit", disabled: isLoading, className: "w-full px-4 py-2 bg-accent text-white rounded font-medium hover:bg-accent-dark transition-smooth disabled:opacity-50", children: isLoading ? 'Sending...' : 'Send reset link' }), _jsx("div", { className: "text-center", children: _jsx(Link, { to: "/auth/login", className: "text-sm text-accent hover:underline", children: "Back to login" }) })] }) }));
}
