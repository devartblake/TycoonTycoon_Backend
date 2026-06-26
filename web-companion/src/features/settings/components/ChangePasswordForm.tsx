/**
 * Change Password Form Component
 * Allows authenticated users to change their password with validation
 */

import { useState } from 'react';
import { apiClient } from '@core/api/client';
import { Lock, Eye, EyeOff, CheckCircle, AlertCircle, Loader } from 'lucide-react';

interface PasswordRequirements {
  minLength: boolean;
  hasUppercase: boolean;
  hasLowercase: boolean;
  hasNumber: boolean;
  hasSpecial: boolean;
}

export function ChangePasswordForm() {
  const [formData, setFormData] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });

  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [showPasswords, setShowPasswords] = useState({
    current: false,
    new: false,
    confirm: false,
  });

  // Validate password against requirements
  const validatePassword = (password: string): PasswordRequirements => {
    return {
      minLength: password.length >= 8,
      hasUppercase: /[A-Z]/.test(password),
      hasLowercase: /[a-z]/.test(password),
      hasNumber: /[0-9]/.test(password),
      hasSpecial: /[!@#$%^&*(),.?":{}|<>]/.test(password),
    };
  };

  const passwordRequirements = validatePassword(formData.newPassword);
  const passwordValid = Object.values(passwordRequirements).every(Boolean);
  const passwordsMatch =
    formData.newPassword && formData.confirmPassword === formData.newPassword;
  const formValid =
    formData.currentPassword && formData.newPassword && passwordValid && passwordsMatch;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccess(false);

    if (!formValid) {
      setError('Please fix all errors above');
      return;
    }

    setIsLoading(true);

    try {
      await apiClient.changePassword(
        formData.currentPassword,
        formData.newPassword
      );

      // Success
      setSuccess(true);
      setFormData({
        currentPassword: '',
        newPassword: '',
        confirmPassword: '',
      });

      // Clear success message after 3 seconds
      setTimeout(() => setSuccess(false), 3000);
    } catch (err: any) {
      console.error('Password change error:', err);
      const errorMessage =
        err.response?.data?.message ||
        err.response?.data?.error ||
        err.message ||
        'Failed to change password. Please try again.';
      setError(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
    // Clear error when user starts typing
    if (error) {
      setError(null);
    }
  };

  const togglePasswordVisibility = (field: 'current' | 'new' | 'confirm') => {
    setShowPasswords((prev) => ({
      ...prev,
      [field]: !prev[field],
    }));
  };

  return (
    <div className="bg-gray-900 rounded-lg border border-gray-800 p-6 max-w-md w-full">
      <h3 className="text-lg font-bold text-white mb-4">Change Password</h3>

      {/* Error Alert */}
      {error && (
        <div className="mb-4 p-3 bg-red-900/20 border border-red-700 rounded-lg flex items-start gap-3">
          <AlertCircle size={18} className="text-red-400 flex-shrink-0 mt-0.5" />
          <p className="text-red-300 text-sm">{error}</p>
        </div>
      )}

      {/* Success Alert */}
      {success && (
        <div className="mb-4 p-3 bg-green-900/20 border border-green-700 rounded-lg flex items-start gap-3">
          <CheckCircle size={18} className="text-green-400 flex-shrink-0 mt-0.5" />
          <p className="text-green-300 text-sm">Password changed successfully!</p>
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Current Password Field */}
        <div>
          <label htmlFor="currentPassword" className="block text-sm font-medium text-gray-300 mb-2">
            Current Password
          </label>
          <div className="relative">
            <Lock
              size={18}
              className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500"
            />
            <input
              id="currentPassword"
              type={showPasswords.current ? 'text' : 'password'}
              name="currentPassword"
              value={formData.currentPassword}
              onChange={handleChange}
              placeholder="Enter your current password"
              className="w-full pl-10 pr-10 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
              required
              disabled={isLoading}
            />
            <button
              type="button"
              onClick={() => togglePasswordVisibility('current')}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 hover:text-gray-400 transition-colors"
              tabIndex={-1}
            >
              {showPasswords.current ? <EyeOff size={18} /> : <Eye size={18} />}
            </button>
          </div>
        </div>

        {/* New Password Field */}
        <div>
          <label htmlFor="newPassword" className="block text-sm font-medium text-gray-300 mb-2">
            New Password
          </label>
          <div className="relative">
            <Lock
              size={18}
              className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500"
            />
            <input
              id="newPassword"
              type={showPasswords.new ? 'text' : 'password'}
              name="newPassword"
              value={formData.newPassword}
              onChange={handleChange}
              placeholder="Enter your new password"
              className="w-full pl-10 pr-10 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
              required
              disabled={isLoading}
            />
            <button
              type="button"
              onClick={() => togglePasswordVisibility('new')}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 hover:text-gray-400 transition-colors"
              tabIndex={-1}
            >
              {showPasswords.new ? <EyeOff size={18} /> : <Eye size={18} />}
            </button>
          </div>

          {/* Password Strength Indicator */}
          {formData.newPassword && (
            <div className="mt-3 p-3 bg-gray-800/50 rounded-lg space-y-1">
              <p className="text-xs font-semibold text-gray-400 mb-2">Password Requirements:</p>
              {[
                {
                  label: '8+ characters',
                  valid: passwordRequirements.minLength,
                },
                {
                  label: 'Uppercase letter (A-Z)',
                  valid: passwordRequirements.hasUppercase,
                },
                {
                  label: 'Lowercase letter (a-z)',
                  valid: passwordRequirements.hasLowercase,
                },
                {
                  label: 'Number (0-9)',
                  valid: passwordRequirements.hasNumber,
                },
                {
                  label: 'Special character (!@#$%...)',
                  valid: passwordRequirements.hasSpecial,
                },
              ].map(({ label, valid }) => (
                <div key={label} className="flex items-center gap-2">
                  <div
                    className={`w-4 h-4 rounded-full flex-shrink-0 transition-colors ${
                      valid ? 'bg-green-500' : 'bg-gray-600'
                    }`}
                  />
                  <span
                    className={`text-xs transition-colors ${
                      valid ? 'text-green-400' : 'text-gray-400'
                    }`}
                  >
                    {label}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Confirm Password Field */}
        <div>
          <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-300 mb-2">
            Confirm Password
          </label>
          <div className="relative">
            <Lock
              size={18}
              className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500"
            />
            <input
              id="confirmPassword"
              type={showPasswords.confirm ? 'text' : 'password'}
              name="confirmPassword"
              value={formData.confirmPassword}
              onChange={handleChange}
              placeholder="Confirm your new password"
              className="w-full pl-10 pr-10 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
              required
              disabled={isLoading}
            />
            <button
              type="button"
              onClick={() => togglePasswordVisibility('confirm')}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 hover:text-gray-400 transition-colors"
              tabIndex={-1}
            >
              {showPasswords.confirm ? <EyeOff size={18} /> : <Eye size={18} />}
            </button>
          </div>
          {formData.confirmPassword && !passwordsMatch && (
            <p className="text-xs text-red-400 mt-1">Passwords do not match</p>
          )}
          {formData.confirmPassword && passwordsMatch && (
            <p className="text-xs text-green-400 mt-1">Passwords match ✓</p>
          )}
        </div>

        {/* Submit Button */}
        <button
          type="submit"
          disabled={!formValid || isLoading}
          className="w-full py-2 px-4 bg-primary hover:bg-secondary text-white font-medium rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2 mt-6"
        >
          {isLoading && <Loader size={18} className="animate-spin" />}
          {isLoading ? 'Changing password...' : 'Change Password'}
        </button>
      </form>

      {/* Security Info */}
      <div className="mt-4 p-3 bg-blue-900/20 border border-blue-700 rounded-lg">
        <p className="text-xs text-blue-300">
          💡 <strong>Tip:</strong> Use a strong, unique password that you don't use on other websites.
        </p>
      </div>
    </div>
  );
}

export default ChangePasswordForm;
