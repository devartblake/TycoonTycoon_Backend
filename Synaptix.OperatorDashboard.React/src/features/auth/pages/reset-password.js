import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Reset password page
 */
import { useState, useEffect } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { adminResetPassword, adminValidateResetToken } from '@/features/auth/api';
const resetPasswordSchema = z
    .object({
    newPassword: z.string().min(8, 'Password must be at least 8 characters'),
    confirmPassword: z.string(),
})
    .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
});
export default function ResetPasswordPage() {
    const [searchParams] = useSearchParams();
    const token = searchParams.get('token');
    const [isLoading, setIsLoading] = useState(false);
    const [isValidating, setIsValidating] = useState(true);
    const [isSubmitted, setIsSubmitted] = useState(false);
    const [error, setError] = useState(null);
    const [tokenError, setTokenError] = useState(null);
    const { register, handleSubmit, formState: { errors }, } = useForm({
        resolver: zodResolver(resetPasswordSchema),
    });
    // Validate token on mount
    useEffect(() => {
        const validateToken = async () => {
            if (!token) {
                setTokenError('Reset token is missing. Please use the link from your email.');
                setIsValidating(false);
                return;
            }
            try {
                await adminValidateResetToken(token);
                setIsValidating(false);
            }
            catch (err) {
                const errorMessage = err instanceof Error ? err.message : 'Invalid or expired reset token.';
                setTokenError(errorMessage);
                setIsValidating(false);
            }
        };
        validateToken();
    }, [token]);
    const onSubmit = async (data) => {
        if (!token)
            return;
        setIsLoading(true);
        setError(null);
        try {
            await adminResetPassword(token, data.newPassword, data.confirmPassword);
            setIsSubmitted(true);
        }
        catch (err) {
            const errorMessage = err instanceof Error ? err.message : 'Failed to reset password.';
            setError(errorMessage);
        }
        finally {
            setIsLoading(false);
        }
    };
    if (isValidating) {
        return (_jsx("div", { className: "space-y-4 text-center", children: _jsx("h2", { className: "text-2xl font-bold text-ink-primary", children: "Validating reset link..." }) }));
    }
    if (tokenError) {
        return (_jsxs("div", { className: "space-y-4", children: [_jsx("div", { className: "p-4 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm", children: tokenError }), _jsx("div", { className: "text-center", children: _jsx(Link, { to: "/auth/forgot-password", className: "text-sm text-accent hover:underline", children: "Request a new reset link" }) })] }));
    }
    if (isSubmitted) {
        return (_jsxs("div", { className: "space-y-4", children: [_jsx("h2", { className: "text-2xl font-bold text-center text-ink-primary", children: "Password reset successful" }), _jsx("p", { className: "text-center text-ink-secondary", children: "Your password has been reset. You can now log in with your new password." }), _jsx("div", { className: "text-center pt-4", children: _jsx(Link, { to: "/auth/login", className: "text-sm text-accent hover:underline", children: "Go to login" }) })] }));
    }
    return (_jsxs("form", { onSubmit: handleSubmit(onSubmit), className: "space-y-4", children: [_jsx("h2", { className: "text-2xl font-bold text-center text-ink-primary mb-6", children: "Reset your password" }), error && (_jsx("div", { className: "p-4 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm", children: error })), _jsxs("div", { children: [_jsx("label", { htmlFor: "newPassword", className: "block text-sm font-medium text-ink-primary mb-1", children: "New password" }), _jsx("input", { ...register('newPassword'), id: "newPassword", type: "password", className: "w-full px-3 py-2 border border-panel-border rounded focus-ring", placeholder: "\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022" }), errors.newPassword && (_jsx("p", { className: "text-xs text-status-offline mt-1", children: errors.newPassword.message }))] }), _jsxs("div", { children: [_jsx("label", { htmlFor: "confirmPassword", className: "block text-sm font-medium text-ink-primary mb-1", children: "Confirm password" }), _jsx("input", { ...register('confirmPassword'), id: "confirmPassword", type: "password", className: "w-full px-3 py-2 border border-panel-border rounded focus-ring", placeholder: "\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022" }), errors.confirmPassword && (_jsx("p", { className: "text-xs text-status-offline mt-1", children: errors.confirmPassword.message }))] }), _jsx("button", { type: "submit", disabled: isLoading, className: "w-full px-4 py-2 bg-accent text-white rounded font-medium hover:bg-accent-dark transition-smooth disabled:opacity-50", children: isLoading ? 'Resetting...' : 'Reset password' }), _jsx("div", { className: "text-center", children: _jsx(Link, { to: "/auth/login", className: "text-sm text-accent hover:underline", children: "Back to login" }) })] }));
}
