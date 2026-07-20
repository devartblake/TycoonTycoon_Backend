/**
 * Forgot Password Page - Multi-step OTP-based password reset
 * Steps: 1) Email Entry 2) Method Selection 3) OTP Verification 4) New Password
 */

import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { apiClient } from '@core/api/client';
import { AlertCircle, CheckCircle, ArrowLeft, Loader } from 'lucide-react';

type Step = 'email' | 'method' | 'otp' | 'password' | 'success';

export function ForgotPasswordPage() {
  const navigate = useNavigate();

  // Step tracking
  const [step, setStep] = useState<Step>('email');

  // Form data
  const [email, setEmail] = useState('');
  const [selectedMethod] = useState<'email' | 'sms'>('email');
  const [otp, setOtp] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [resetToken, setResetToken] = useState('');

  // UI state
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [attemptsRemaining, setAttemptsRemaining] = useState(5);
  const [canResendOtp, setCanResendOtp] = useState(false);
  const [resendCountdown, setResendCountdown] = useState(0);

  // Password strength
  const [passwordRequirements, setPasswordRequirements] = useState({
    minLength: false,
    hasUppercase: false,
    hasLowercase: false,
    hasNumber: false,
    hasSpecial: false,
  });

  const validatePassword = (password: string) => {
    setPasswordRequirements({
      minLength: password.length >= 8,
      hasUppercase: /[A-Z]/.test(password),
      hasLowercase: /[a-z]/.test(password),
      hasNumber: /[0-9]/.test(password),
      hasSpecial: /[!@#$%^&*(),.?":{}|<>]/.test(password),
    });
  };

  const handleEmailSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      await apiClient.post('/auth/forgot-password', {
        email,
        method: 'email',
      });

      setSuccessMessage(`OTP sent to ${email}`);
      setStep('otp');
    } catch (err: any) {
      const errorMsg = err.response?.data?.message || err.message || 'Failed to send OTP';
      setError(errorMsg);
    } finally {
      setIsLoading(false);
    }
  };

  const handleOtpSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      const response = await apiClient.post('/auth/verify-otp', {
        email,
        otp,
      });

      setResetToken(response.data.resetToken);
      setOtp(''); // Clear OTP from form
      setSuccessMessage('OTP verified successfully');
      setStep('password');
    } catch (err: any) {
      const errorMsg = err.response?.data?.message || err.message || 'Invalid OTP';
      setError(errorMsg);
      setAttemptsRemaining(err.response?.data?.attemptsRemaining || 0);
    } finally {
      setIsLoading(false);
    }
  };

  const handlePasswordSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      await apiClient.post('/auth/reset-password', {
        email,
        token: resetToken,
        newPassword,
      });

      setSuccessMessage('Password reset successfully!');
      setStep('success');

      // Redirect to login after 3 seconds
      setTimeout(() => {
        navigate('/login');
      }, 3000);
    } catch (err: any) {
      const errorMsg = err.response?.data?.message || err.message || 'Failed to reset password';
      setError(errorMsg);
    } finally {
      setIsLoading(false);
    }
  };

  const handleResendOtp = async () => {
    setError(null);
    setIsLoading(true);

    try {
      await apiClient.post('/auth/forgot-password', {
        email,
        method: selectedMethod,
      });

      setSuccessMessage(`OTP resent to your ${selectedMethod}`);
      setOtp('');
      setCanResendOtp(false);
      setResendCountdown(60);

      // Countdown timer
      const timer = setInterval(() => {
        setResendCountdown((prev) => {
          if (prev <= 1) {
            clearInterval(timer);
            setCanResendOtp(true);
            return 0;
          }
          return prev - 1;
        });
      }, 1000);
    } catch (err: any) {
      const errorMsg = err.response?.data?.message || err.message || 'Failed to resend OTP';
      setError(errorMsg);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-950 to-gray-900 flex items-center justify-center px-4 py-8">
      <div className="w-full max-w-md">
        {/* Header */}
        <div className="text-center mb-8">
          <h1 className="text-4xl font-bold text-white mb-2">Trivia Tycoon</h1>
          <p className="text-gray-400">Reset Your Password</p>
        </div>

        {/* Progress indicator */}
        <div className="mb-8">
          <div className="flex justify-between items-center">
            <div className={`flex items-center justify-center w-10 h-10 rounded-full ${step === 'email' || step === 'otp' || step === 'password' || step === 'success' ? 'bg-primary text-white' : 'bg-gray-700 text-gray-400'}`}>
              1
            </div>
            <div className="flex-1 h-1 mx-2 bg-gray-700"></div>
            <div className={`flex items-center justify-center w-10 h-10 rounded-full ${step === 'otp' || step === 'password' || step === 'success' ? 'bg-primary text-white' : 'bg-gray-700 text-gray-400'}`}>
              2
            </div>
            <div className="flex-1 h-1 mx-2 bg-gray-700"></div>
            <div className={`flex items-center justify-center w-10 h-10 rounded-full ${step === 'password' || step === 'success' ? 'bg-primary text-white' : 'bg-gray-700 text-gray-400'}`}>
              3
            </div>
          </div>
          <div className="flex justify-between text-xs text-gray-400 mt-2">
            <span>Email</span>
            <span>OTP</span>
            <span>Password</span>
          </div>
        </div>

        {/* Card */}
        <div className="bg-gray-900 rounded-lg border border-gray-800 p-8 shadow-xl">
          {/* Error Alert */}
          {error && (
            <div className="mb-6 p-4 bg-red-900/20 border border-red-800 rounded-lg flex items-start gap-3">
              <AlertCircle size={20} className="text-red-400 flex-shrink-0 mt-0.5" />
              <p className="text-red-300 text-sm">{error}</p>
            </div>
          )}

          {/* Success Alert */}
          {successMessage && (
            <div className="mb-6 p-4 bg-green-900/20 border border-green-800 rounded-lg flex items-start gap-3">
              <CheckCircle size={20} className="text-green-400 flex-shrink-0 mt-0.5" />
              <p className="text-green-300 text-sm">{successMessage}</p>
            </div>
          )}

          {/* Step 1: Email Entry */}
          {step === 'email' && (
            <form onSubmit={handleEmailSubmit} className="space-y-4">
              <h2 className="text-2xl font-bold text-white mb-4">Enter Your Email</h2>
              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Email Address
                </label>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="your@email.com"
                  className="w-full px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
                  required
                  disabled={isLoading}
                />
              </div>
              <button
                type="submit"
                disabled={isLoading || !email}
                className="w-full py-2 px-4 bg-primary hover:bg-secondary text-white font-medium rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
              >
                {isLoading && <Loader size={18} className="animate-spin" />}
                {isLoading ? 'Sending...' : 'Send OTP'}
              </button>
            </form>
          )}

          {/* Step 2: OTP Verification */}
          {step === 'otp' && (
            <form onSubmit={handleOtpSubmit} className="space-y-4">
              <h2 className="text-2xl font-bold text-white mb-4">Enter OTP Code</h2>
              <p className="text-gray-400 text-sm">We sent a 6-digit code to {email}</p>
              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  One-Time Password
                </label>
                <input
                  type="text"
                  value={otp}
                  onChange={(e) => setOtp(e.target.value.replace(/\D/g, '').slice(0, 6))}
                  placeholder="000000"
                  maxLength={6}
                  className="w-full px-4 py-3 bg-gray-800 border border-gray-700 rounded-lg text-white text-center text-2xl tracking-widest font-mono focus:outline-none focus:border-primary transition-colors"
                  required
                  disabled={isLoading}
                />
                <p className="text-xs text-gray-400 mt-2">
                  {attemptsRemaining} attempts remaining
                </p>
              </div>
              <button
                type="submit"
                disabled={isLoading || otp.length !== 6}
                className="w-full py-2 px-4 bg-primary hover:bg-secondary text-white font-medium rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
              >
                {isLoading && <Loader size={18} className="animate-spin" />}
                {isLoading ? 'Verifying...' : 'Verify OTP'}
              </button>
              <button
                type="button"
                onClick={handleResendOtp}
                disabled={isLoading || !canResendOtp}
                className="w-full text-center text-sm text-gray-400 hover:text-gray-300 transition-colors"
              >
                {resendCountdown > 0 ? `Resend in ${resendCountdown}s` : 'Didn\'t receive code? Resend'}
              </button>
            </form>
          )}

          {/* Step 3: New Password */}
          {step === 'password' && (
            <form onSubmit={handlePasswordSubmit} className="space-y-4">
              <h2 className="text-2xl font-bold text-white mb-4">Create New Password</h2>
              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  New Password
                </label>
                <input
                  type="password"
                  value={newPassword}
                  onChange={(e) => {
                    setNewPassword(e.target.value);
                    validatePassword(e.target.value);
                  }}
                  placeholder="••••••••"
                  className="w-full px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
                  required
                  disabled={isLoading}
                />
                {newPassword && (
                  <div className="mt-3 p-3 bg-gray-800/50 rounded-lg space-y-1">
                    {[
                      { label: '8+ characters', valid: passwordRequirements.minLength },
                      { label: 'Uppercase (A-Z)', valid: passwordRequirements.hasUppercase },
                      { label: 'Lowercase (a-z)', valid: passwordRequirements.hasLowercase },
                      { label: 'Number (0-9)', valid: passwordRequirements.hasNumber },
                      { label: 'Special character (!@#...)', valid: passwordRequirements.hasSpecial },
                    ].map(({ label, valid }) => (
                      <div key={label} className="flex items-center gap-2">
                        <div className={`w-4 h-4 rounded-full ${valid ? 'bg-green-500' : 'bg-gray-600'}`} />
                        <span className={`text-xs ${valid ? 'text-green-400' : 'text-gray-400'}`}>
                          {label}
                        </span>
                      </div>
                    ))}
                  </div>
                )}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Confirm Password
                </label>
                <input
                  type="password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  placeholder="••••••••"
                  className="w-full px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-primary transition-colors"
                  required
                  disabled={isLoading}
                />
                {confirmPassword && (
                  <p className={`text-xs mt-1 ${newPassword === confirmPassword ? 'text-green-400' : 'text-red-400'}`}>
                    {newPassword === confirmPassword ? '✓ Passwords match' : '✗ Passwords don\'t match'}
                  </p>
                )}
              </div>
              <button
                type="submit"
                disabled={isLoading || newPassword !== confirmPassword || !Object.values(passwordRequirements).every(Boolean)}
                className="w-full py-2 px-4 bg-primary hover:bg-secondary text-white font-medium rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
              >
                {isLoading && <Loader size={18} className="animate-spin" />}
                {isLoading ? 'Resetting...' : 'Reset Password'}
              </button>
            </form>
          )}

          {/* Success */}
          {step === 'success' && (
            <div className="text-center">
              <div className="mb-4">
                <CheckCircle size={64} className="text-green-400 mx-auto" />
              </div>
              <h2 className="text-2xl font-bold text-white mb-2">Password Reset!</h2>
              <p className="text-gray-400 mb-6">Your password has been reset successfully. Redirecting to login...</p>
            </div>
          )}
        </div>

        {/* Back to login link */}
        {step !== 'success' && (
          <div className="mt-4 text-center">
            <Link to="/login" className="inline-flex items-center gap-2 text-gray-400 hover:text-gray-300 transition-colors">
              <ArrowLeft size={18} />
              Back to login
            </Link>
          </div>
        )}
      </div>
    </div>
  );
}

export default ForgotPasswordPage;
