using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BCrypt.Net;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Api.Services
{
    /// <summary>
    /// Service for generating, storing, and verifying OTP (One-Time Password) tokens
    /// Used for password reset and email verification flows
    /// </summary>
    public class OtpService
    {
        private readonly IAppDb _database;
        private readonly IConfiguration _config;

        public OtpService(IAppDb database, IConfiguration config)
        {
            _database = database;
            _config = config;
        }

        /// <summary>
        /// Generates a random OTP (One-Time Password)
        /// Default length: 6 digits
        /// </summary>
        public string GenerateOtp()
        {
            int otpLength = _config.GetValue<int>("Otp:Length", 6);
            byte[] tokenData = new byte[4];
            RandomNumberGenerator.Fill(tokenData);
            int randomNumber = BitConverter.ToInt32(tokenData, 0) & Int32.MaxValue;
            int otp = randomNumber % (int)Math.Pow(10, otpLength);
            return otp.ToString().PadLeft(otpLength, '0');
        }

        /// <summary>
        /// Hashes an OTP using bcrypt
        /// </summary>
        public string HashOtp(string otp)
        {
            return BCrypt.Net.BCrypt.HashPassword(otp);
        }

        /// <summary>
        /// Verifies an OTP against a bcrypt hash
        /// </summary>
        public bool VerifyOtpHash(string otp, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(otp, hash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Stores an OTP hash in the database
        /// Clears any previous OTPs for the same email
        /// </summary>
        public async Task<bool> StoreOtpAsync(string email, string otpHash, CancellationToken cancellation)
        {
            try
            {
                email = email.ToLowerInvariant();

                // Clear old OTPs for this email
                var oldOtps = await _database.OtpTokens
                    .Where(o => o.Email == email)
                    .ToListAsync(cancellation);

                if (oldOtps.Any())
                {
                    _database.OtpTokens.RemoveRange(oldOtps);
                }

                // Calculate expiration
                int expirationMinutes = _config.GetValue<int>("Otp:ExpirationMinutes", 10);
                var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes);

                // Create new OTP token
                var otpToken = new OtpToken(email, otpHash, expiresAt);
                _database.OtpTokens.Add(otpToken);

                await _database.SaveChangesAsync(cancellation);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to store OTP: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verifies an OTP for a given email
        /// Checks expiration and usage status
        /// Marks OTP as used on success
        /// </summary>
        public async Task<bool> VerifyOtpAsync(string email, string otp, CancellationToken cancellation)
        {
            try
            {
                email = email.ToLowerInvariant();

                var otpToken = await _database.OtpTokens
                    .FirstOrDefaultAsync(o => o.Email == email, cancellation);

                if (otpToken == null)
                {
                    return false;
                }

                // Check if already used
                if (otpToken.IsUsed)
                {
                    return false;
                }

                // Check if expired
                if (otpToken.IsExpired)
                {
                    return false;
                }

                // Check max attempts
                int maxAttempts = _config.GetValue<int>("Otp:MaxAttempts", 5);
                if (otpToken.VerificationAttempts >= maxAttempts)
                {
                    return false;
                }

                // Verify OTP hash
                if (!VerifyOtpHash(otp, otpToken.OtpHash))
                {
                    otpToken.VerificationAttempts++;
                    await _database.SaveChangesAsync(cancellation);
                    return false;
                }

                // Mark as used
                otpToken.IsUsed = true;
                otpToken.VerificationAttempts++;
                await _database.SaveChangesAsync(cancellation);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to verify OTP: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the number of remaining verification attempts for an email
        /// </summary>
        public async Task<int> GetRemainingAttemptsAsync(string email, CancellationToken cancellation)
        {
            try
            {
                email = email.ToLowerInvariant();
                var otpToken = await _database.OtpTokens
                    .FirstOrDefaultAsync(o => o.Email == email, cancellation);

                if (otpToken == null)
                    return 0;

                int maxAttempts = _config.GetValue<int>("Otp:MaxAttempts", 5);
                return Math.Max(0, maxAttempts - otpToken.VerificationAttempts);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Clears all OTP tokens for an email
        /// </summary>
        public async Task<bool> ClearOtpAsync(string email, CancellationToken cancellation)
        {
            try
            {
                email = email.ToLowerInvariant();
                var otps = await _database.OtpTokens
                    .Where(o => o.Email == email)
                    .ToListAsync(cancellation);

                if (otps.Any())
                {
                    _database.OtpTokens.RemoveRange(otps);
                    await _database.SaveChangesAsync(cancellation);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to clear OTP: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if an email has exceeded the rate limit for OTP requests
        /// </summary>
        public async Task<bool> IsRateLimitedAsync(string email, CancellationToken cancellation)
        {
            try
            {
                email = email.ToLowerInvariant();
                int rateLimitPerHour = _config.GetValue<int>("Otp:RateLimitPerHour", 3);

                var otpsInLastHour = await _database.OtpTokens
                    .Where(o => o.Email == email &&
                                o.CreatedAt > DateTimeOffset.UtcNow.AddHours(-1))
                    .CountAsync(cancellation);

                return otpsInLastHour >= rateLimitPerHour;
            }
            catch
            {
                return false;
            }
        }
    }
}
