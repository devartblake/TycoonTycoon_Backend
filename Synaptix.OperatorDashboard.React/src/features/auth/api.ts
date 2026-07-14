/**
 * Authentication API client
 *
 * Aligned to backend AdminAuthEndpoints + Django admin_auth_client:
 *   POST /admin/auth/login
 *   POST /admin/auth/refresh
 *   GET  /admin/auth/me
 *   POST /admin/auth/forgot-password
 *   POST /admin/auth/reset-password
 *   POST /admin/auth/validate-reset-token
 *
 * Transport is plain JSON via Vite/nginx proxy (X-Admin-Ops-Key injected at
 * the edge). Django may optionally use KMS secure-channel; React does not yet.
 */

import { apiPost, apiGet } from '@/lib/api-client'
import type {
  LoginRequest,
  LoginResponse,
  RefreshTokenResponse,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  ValidateResetTokenRequest,
  ValidateResetTokenResponse,
  AdminProfile,
} from '@/types/auth'

const AUTH_API_PREFIX = '/admin/auth'

/**
 * Login with email and password
 */
export async function adminLogin(email: string, password: string): Promise<LoginResponse> {
  return apiPost<LoginResponse>(`${AUTH_API_PREFIX}/login`, {
    email,
    password,
  } as LoginRequest)
}

/**
 * Get current user profile
 */
export async function adminMe(): Promise<AdminProfile> {
  return apiGet<AdminProfile>(`${AUTH_API_PREFIX}/me`)
}

/**
 * Refresh access token using refresh token
 */
export async function adminRefresh(refreshToken: string): Promise<RefreshTokenResponse> {
  return apiPost<RefreshTokenResponse>(`${AUTH_API_PREFIX}/refresh`, {
    refreshToken,
  })
}

/**
 * Initiate password reset (sends email with reset link)
 */
export async function adminForgotPassword(email: string): Promise<{ message: string }> {
  return apiPost(`${AUTH_API_PREFIX}/forgot-password`, {
    email,
  } as ForgotPasswordRequest)
}

/**
 * Reset password with reset token
 */
export async function adminResetPassword(
  token: string,
  newPassword: string,
  confirmPassword: string
): Promise<{ message: string }> {
  return apiPost(`${AUTH_API_PREFIX}/reset-password`, {
    token,
    newPassword,
    confirmPassword,
  } as ResetPasswordRequest)
}

/**
 * Validate reset token (check if it's valid and not expired)
 */
export async function adminValidateResetToken(
  token: string
): Promise<ValidateResetTokenResponse> {
  return apiPost<ValidateResetTokenResponse>(`${AUTH_API_PREFIX}/validate-reset-token`, {
    token,
  } as ValidateResetTokenRequest)
}
