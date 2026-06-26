/**
 * Sign up page for new users
 */

import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuthStore } from '@stores';
import { apiClient } from '@core/api/client';
import { Mail, Lock, User, AlertCircle, CheckCircle } from 'lucide-react';

export function SignupPage() {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    displayName: '',
  });
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [validations, setValidations] = useState({
    email: false,
    password: false,
    passwordMatch: false,
    displayName: false,
  });

  const setUser = useAuthStore((state) => state.setUser);
  const setLoading = useAuthStore((state) => state.setLoading);

  // Validate form in real-time
  const validateForm = (data: typeof formData) => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const newValidations = {
      email: emailRegex.test(data.email),
      password: data.password.length >= 8,
      passwordMatch: data.password === data.confirmPassword && data.password.length > 0,
      displayName: data.displayName.length >= 2,
    };
    setValidations(newValidations);
    return Object.values(newValidations).every(Boolean);
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    const updated = { ...formData, [name]: value };
    setFormData(updated);
    setError(null);
    validateForm(updated);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!validateForm(formData)) {
      setError('Please fix the errors above');
      return;
    }

    setIsLoading(true);
    setLoading(true);

    try {
      const response = await apiClient.signup(
        formData.email,
        formData.password,
        formData.displayName
      );
      const { user: backendUser, token, refreshToken } = response;

      // Store tokens
      localStorage.setItem('auth_token', token);
      if (refreshToken) {
        localStorage.setItem('refresh_token', refreshToken);
      }

      // Transform backend UserDto to frontend User
      const user = {
        id: backendUser.id,
        email: backendUser.email,
        displayName: backendUser.handle || formData.displayName,
        avatar: backendUser.avatarUrl || undefined,
        role: (backendUser.userRoles?.[0] || 'user').toLowerCase() as 'user' | 'admin',
        createdAt: new Date().toISOString(),
      };

      // Update auth store
      setUser(user);
      navigate('/');
    } catch (err: any) {
      console.error('Signup error:', err);
      const errorMessage =
        err.response?.data?.message ||
        err.message ||
        'Sign up failed. Please try again.';
      setError(errorMessage);
    } finally {
      setIsLoading(false);
      setLoading(false);
    }
  };

  const isFormValid = Object.values(validations).every(Boolean);

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-950 to-gray-900 flex items-center justify-center px-4 py-8">
      <div className="w-full max-w-md">
        {/* Logo / Header */}
        <div className="text-center mb-8">
          <h1 className="text-4xl font-bold text-white mb-2">Synaptix</h1>
          <p className="text-gray-400">Join the knowledge competition</p>
        </div>

        {/* Card */}
        <div className="bg-gray-900 rounded-lg border border-gray-800 p-8 shadow-xl">
          <h2 className="text-2xl font-bold text-white mb-6">Create Account</h2>

          {/* Error message */}
          {error && (
            <div className="mb-6 p-4 bg-red-900/20 border border-red-800 rounded-lg flex items-start gap-3">
              <AlertCircle size={20} className="text-red-400 flex-shrink-0 mt-0.5" />
              <p className="text-red-300 text-sm">{error}</p>
            </div>
          )}

          {/* Form */}
          <form onSubmit={handleSubmit} className="space-y-4 mb-6">
            {/* Display Name */}
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Display Name
              </label>
              <div className="relative">
                <User
                  size={18}
                  className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-500"
                />
                <input
                  type="text"
                  name="displayName"
                  value={formData.displayName}
                  onChange={handleChange}
                  placeholder="Your name"
                  className="w-full pl-10 pr-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
                  required
                  disabled={isLoading}
                />
              </div>
              <div className="mt-1 flex items-center gap-2">
                {validations.displayName ? (
                  <CheckCircle size={14} className="text-green-500" />
                ) : (
                  <div className="w-3.5 h-3.5 rounded-full border border-gray-600" />
                )}
                <span className="text-xs text-gray-400">Minimum 2 characters</span>
              </div>
            </div>

            {/* Email */}
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">Email</label>
              <div className="relative">
                <Mail
                  size={18}
                  className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-500"
                />
                <input
                  type="email"
                  name="email"
                  value={formData.email}
                  onChange={handleChange}
                  placeholder="you@example.com"
                  className="w-full pl-10 pr-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
                  required
                  disabled={isLoading}
                />
              </div>
              <div className="mt-1 flex items-center gap-2">
                {validations.email ? (
                  <CheckCircle size={14} className="text-green-500" />
                ) : (
                  <div className="w-3.5 h-3.5 rounded-full border border-gray-600" />
                )}
                <span className="text-xs text-gray-400">Valid email required</span>
              </div>
            </div>

            {/* Password */}
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">Password</label>
              <div className="relative">
                <Lock
                  size={18}
                  className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-500"
                />
                <input
                  type="password"
                  name="password"
                  value={formData.password}
                  onChange={handleChange}
                  placeholder="••••••••"
                  className="w-full pl-10 pr-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
                  required
                  disabled={isLoading}
                />
              </div>
              <div className="mt-1 flex items-center gap-2">
                {validations.password ? (
                  <CheckCircle size={14} className="text-green-500" />
                ) : (
                  <div className="w-3.5 h-3.5 rounded-full border border-gray-600" />
                )}
                <span className="text-xs text-gray-400">Minimum 8 characters</span>
              </div>
            </div>

            {/* Confirm Password */}
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">
                Confirm Password
              </label>
              <div className="relative">
                <Lock
                  size={18}
                  className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-500"
                />
                <input
                  type="password"
                  name="confirmPassword"
                  value={formData.confirmPassword}
                  onChange={handleChange}
                  placeholder="••••••••"
                  className="w-full pl-10 pr-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
                  required
                  disabled={isLoading}
                />
              </div>
              <div className="mt-1 flex items-center gap-2">
                {validations.passwordMatch ? (
                  <CheckCircle size={14} className="text-green-500" />
                ) : (
                  <div className="w-3.5 h-3.5 rounded-full border border-gray-600" />
                )}
                <span className="text-xs text-gray-400">Passwords must match</span>
              </div>
            </div>

            {/* Submit Button */}
            <button
              type="submit"
              disabled={!isFormValid || isLoading}
              className="w-full py-2 px-4 bg-primary hover:bg-secondary text-white font-medium rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed mt-6"
            >
              {isLoading ? 'Creating account...' : 'Sign Up'}
            </button>
          </form>

          {/* Sign in link */}
          <p className="text-center text-gray-400 text-sm">
            Already have an account?{' '}
            <Link to="/login" className="text-primary hover:text-secondary transition-colors">
              Sign in
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}

export default SignupPage;
