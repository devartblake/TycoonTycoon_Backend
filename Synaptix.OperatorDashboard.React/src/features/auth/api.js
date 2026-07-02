/**
 * Authentication API client
 * Wraps admin auth endpoints from the backend
 */
import { apiPost, apiGet } from '@/lib/api-client';
const AUTH_API_PREFIX = '/admin/auth';
/**
 * Login with email and password
 */
export async function adminLogin(email, password) {
    return apiPost(`${AUTH_API_PREFIX}/login`, {
        email,
        password,
    });
}
/**
 * Get current user profile
 */
export async function adminMe() {
    return apiGet(`${AUTH_API_PREFIX}/me`);
}
/**
 * Refresh access token using refresh token
 */
export async function adminRefresh(refreshToken) {
    return apiPost(`${AUTH_API_PREFIX}/refresh`, {
        refreshToken,
    });
}
/**
 * Initiate password reset (sends email with reset link)
 */
export async function adminForgotPassword(email) {
    return apiPost(`${AUTH_API_PREFIX}/forgot-password`, {
        email,
    });
}
/**
 * Reset password with reset token
 */
export async function adminResetPassword(token, newPassword, confirmPassword) {
    return apiPost(`${AUTH_API_PREFIX}/reset-password`, {
        token,
        newPassword,
        confirmPassword,
    });
}
/**
 * Validate reset token (check if it's valid and not expired)
 */
export async function adminValidateResetToken(token) {
    return apiPost(`${AUTH_API_PREFIX}/validate-reset-token`, {
        token,
    });
}
