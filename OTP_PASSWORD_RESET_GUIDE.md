# OTP Password Reset Feature - Implementation Guide

**Date**: June 25, 2026  
**Scope**: Complete password reset flow with OTP via Email and SMS  
**Complexity**: Advanced (multi-step flow, third-party integrations)

## Table of Contents

1. [User Flow](#user-flow)
2. [Architecture](#architecture)
3. [Security Best Practices](#security-best-practices)
4. [Implementation Plan](#implementation-plan)
5. [Backend Setup](#backend-setup)
6. [Frontend Implementation](#frontend-implementation)
7. [API Contracts](#api-contracts)

---

## User Flow

### Scenario: User Forgot Password

```
User on Login Page
  ↓
Clicks "Forgot Password?"
  ↓
Enters Email Address
  ↓
Chooses Delivery Method:
  ├─ Email (default)
  └─ SMS (if phone number on file)
  ↓
Clicks "Send OTP"
  ↓
Backend:
  1. Verify email exists
  2. Generate 6-digit OTP (valid 10 minutes)
  3. Send via email or SMS
  4. Store OTP hash in database
  ↓
User sees: "OTP sent to email/phone"
  ↓
User receives OTP
  ↓
User enters OTP (6 digits)
  ↓
Clicks "Verify OTP"
  ↓
Backend:
  1. Verify OTP matches and not expired
  2. Create temporary reset token
  3. Return token to frontend
  ↓
User sees OTP form disappear
  ↓
User enters new password
  ↓
Clicks "Reset Password"
  ↓
Backend:
  1. Verify reset token is valid
  2. Hash new password
  3. Update in database
  4. Clear OTP and token
  5. Send confirmation email
  ↓
User sees: "Password reset successfully!"
  ↓
User redirected to login
  ↓
User logs in with new password ✅
```

---

## Architecture

### Components

```
Frontend (React/Vite)
├─ src/features/auth/pages/ForgotPasswordPage.tsx (NEW)
│  ├─ Step 1: Email Entry Form
│  ├─ Step 2: Delivery Method Selector
│  ├─ Step 3: OTP Verification Form
│  └─ Step 4: New Password Form
│
└─ src/core/api/client.ts (UPDATED)
   ├─ requestPasswordReset(email, method)
   ├─ verifyOtp(email, otp)
   └─ resetPassword(email, token, newPassword)

Backend (.NET)
├─ Synaptix.Shared.Contracts/Dtos/AuthDtos.cs (UPDATED)
│  ├─ RequestPasswordResetRequest
│  ├─ RequestPasswordResetResponse
│  ├─ VerifyOtpRequest
│  ├─ VerifyOtpResponse
│  ├─ ResetPasswordRequest
│  └─ ResetPasswordResponse
│
├─ Synaptix.Backend.Api/Features/Auth/AuthEndpoints.cs (UPDATED)
│  ├─ POST /auth/forgot-password
│  ├─ POST /auth/verify-otp
│  └─ POST /auth/reset-password
│
├─ Synaptix.Backend.Api/Services/OtpService.cs (NEW)
│  ├─ GenerateOtp()
│  ├─ StoreOtp(email, otp)
│  ├─ VerifyOtp(email, otp)
│  └─ ClearOtp(email)
│
└─ Synaptix.Backend.Api/Services/EmailService.cs (NEW)
   ├─ SendPasswordResetEmail()
   └─ SendPasswordResetSms()

Database
└─ OtpTokens table (NEW)
   ├─ Id (Guid)
   ├─ Email (string)
   ├─ OtpHash (string - bcrypt)
   ├─ CreatedAt (DateTime)
   ├─ ExpiresAt (DateTime)
   └─ IsUsed (bool)

Third-Party Services
├─ SendGrid (Email)
└─ Twilio (SMS - optional)
```

---

## Security Best Practices

### OTP Security

✅ **Generation**
- 6-digit numeric OTP (stronger: alphanumeric)
- Generated using cryptographically secure random
- Hash before storing (never store plain OTP)
- Example: `using System.Security.Cryptography;`

✅ **Storage**
- Store OTP hash in database (bcrypt)
- Store expiration time (10 minutes is standard)
- Link to email address (not user ID to prevent enumeration)
- Mark as "used" after verification

✅ **Verification**
- Compare bcrypt hash (not plain text)
- Check expiration before verification
- Check if already used
- Rate limit verification attempts (max 5 per OTP)
- Clear OTP after successful verification

✅ **Delivery**
- Send OTP via secure channels (email/SMS)
- Never expose OTP in logs
- Never display full OTP on UI (show masked: "123***")
- Confirm delivery with user feedback

✅ **Reset Token**
- Generate temporary token after OTP verification
- Token valid for 5 minutes only
- Token single-use (invalidate after password reset)
- Token tied to email (not user ID)

### Additional Security

✅ **Rate Limiting**
- Max 3 OTP requests per email per hour
- Max 5 OTP verification attempts per hour
- Max 3 password reset attempts per hour

✅ **Account Protection**
- Verify email still exists in system
- Don't reveal if email is registered (generic message)
- Don't auto-login after password reset (user must login manually)

✅ **Audit Logging**
- Log all OTP requests
- Log all OTP verifications (success/failure)
- Log all password resets
- Include IP address and timestamp

✅ **Email/SMS Best Practices**
- Never send password in email
- Never send OTP in plaintext email (use HTML template)
- Include timestamp and validity period
- Include link to report if user didn't request
- Professional HTML template with branding

---

## Implementation Plan

### Phase 1: Backend Setup

**1. Install Dependencies**
```bash
cd Synaptix.Backend.Api
dotnet add package SendGrid
dotnet add package Twilio  # Optional for SMS
```

**2. Add Configuration**
- Add SendGrid API key to appsettings.json
- Add Twilio credentials (optional)
- Add OTP settings (length, expiration, rate limits)

**3. Create DTOs** (AuthDtos.cs)
- RequestPasswordResetRequest
- RequestPasswordResetResponse
- VerifyOtpRequest
- VerifyOtpResponse
- ResetPasswordRequest
- ResetPasswordResponse

**4. Create OtpTokens Table** (Database)
- Migration to create table
- Indexes on email and expiration

**5. Create Services**
- OtpService (generate, store, verify, clear)
- EmailService (send OTP via SendGrid)
- SmsService (send OTP via Twilio - optional)

**6. Add Endpoints** (AuthEndpoints.cs)
- POST /auth/forgot-password
- POST /auth/verify-otp
- POST /auth/reset-password

### Phase 2: Frontend Implementation

**1. Create Pages/Components**
- ForgotPasswordPage.tsx (main page)
- EmailEntryForm.tsx (step 1)
- DeliveryMethodSelector.tsx (step 2)
- OtpVerificationForm.tsx (step 3)
- NewPasswordForm.tsx (step 4)

**2. Add API Methods** (client.ts)
- requestPasswordReset()
- verifyOtp()
- resetPassword()

**3. Add Routing**
- Add /forgot-password route to router
- Link from login page

**4. Add UI/UX**
- Loading states
- Error messages
- Success messages
- Progress indicators
- Resend OTP option

### Phase 3: Testing & Deployment

**1. Testing**
- Unit tests for OTP generation
- Unit tests for OTP verification
- Integration tests for full flow
- Security tests (rate limiting, etc.)
- Email delivery tests

**2. Deployment**
- Deploy backend changes
- Deploy frontend changes
- Test in staging
- Production deployment

---

## Backend Setup Details

### Step 1: Update appsettings.json

```json
{
  "SendGrid": {
    "ApiKey": "SG.xxx...",
    "FromEmail": "noreply@synaptixplay.com",
    "FromName": "Trivia Tycoon"
  },
  "Twilio": {
    "AccountSid": "AC...",
    "AuthToken": "...",
    "FromPhoneNumber": "+1234567890"
  },
  "Otp": {
    "Length": 6,
    "ExpirationMinutes": 10,
    "MaxAttempts": 5,
    "RateLimitPerHour": 3
  }
}
```

### Step 2: OTP Service

```csharp
using System;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

public class OtpService
{
    private readonly IAppDb _database;
    private readonly IConfiguration _config;

    public OtpService(IAppDb database, IConfiguration config)
    {
        _database = database;
        _config = config;
    }

    public string GenerateOtp()
    {
        // Generate 6-digit OTP
        int otpLength = _config.GetValue<int>("Otp:Length", 6);
        using (var rng = new RNGCryptoServiceProvider())
        {
            byte[] tokenData = new byte[4];
            rng.GetBytes(tokenData);
            int randomNumber = BitConverter.ToInt32(tokenData, 0) & Int32.MaxValue;
            int otp = randomNumber % (int)Math.Pow(10, otpLength);
            return otp.ToString().PadLeft(otpLength, '0');
        }
    }

    public string HashOtp(string otp)
    {
        return BCrypt.Net.BCrypt.HashPassword(otp);
    }

    public bool VerifyOtp(string otp, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(otp, hash);
    }

    public async Task<bool> StoreOtpAsync(string email, string otpHash)
    {
        // Clear old OTPs
        var oldOtps = await _database.OtpTokens
            .Where(o => o.Email == email.ToLower())
            .ToListAsync();
        
        foreach (var oldOtp in oldOtps)
            _database.OtpTokens.Remove(oldOtp);

        // Store new OTP
        int expirationMinutes = _config.GetValue<int>("Otp:ExpirationMinutes", 10);
        var otpToken = new OtpToken
        {
            Id = Guid.NewGuid(),
            Email = email.ToLower(),
            OtpHash = otpHash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes),
            IsUsed = false
        };

        _database.OtpTokens.Add(otpToken);
        await _database.SaveChangesAsync();
        return true;
    }

    public async Task<bool> VerifyOtpAsync(string email, string otp)
    {
        var otpToken = await _database.OtpTokens
            .FirstOrDefaultAsync(o => o.Email == email.ToLower());

        if (otpToken == null)
            return false;

        if (otpToken.IsUsed)
            return false;

        if (DateTimeOffset.UtcNow > otpToken.ExpiresAt)
            return false;

        if (!VerifyOtp(otp, otpToken.OtpHash))
            return false;

        // Mark as used
        otpToken.IsUsed = true;
        await _database.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ClearOtpAsync(string email)
    {
        var otps = await _database.OtpTokens
            .Where(o => o.Email == email.ToLower())
            .ToListAsync();

        foreach (var otp in otps)
            _database.OtpTokens.Remove(otp);

        await _database.SaveChangesAsync();
        return true;
    }
}
```

### Step 3: Email Service (SendGrid)

```csharp
using SendGrid;
using SendGrid.Helpers.Mail;

public class EmailService
{
    private readonly SendGridClient _sendGridClient;
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
        var apiKey = config["SendGrid:ApiKey"];
        _sendGridClient = new SendGridClient(apiKey);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string email, string otp, string displayName)
    {
        var from = new EmailAddress(_config["SendGrid:FromEmail"], _config["SendGrid:FromName"]);
        var to = new EmailAddress(email);
        var subject = "Reset Your Trivia Tycoon Password";

        var htmlContent = $@"
            <h2>Password Reset Request</h2>
            <p>Hi {displayName},</p>
            <p>You requested to reset your Trivia Tycoon password. Use this code:</p>
            <h1 style='font-size: 48px; letter-spacing: 5px;'>{otp}</h1>
            <p>This code expires in 10 minutes.</p>
            <p><strong>Didn't request this?</strong> Ignore this email or contact support.</p>
            <hr/>
            <p style='color: #999; font-size: 12px;'>
                Sent at {DateTimeOffset.UtcNow:O}<br/>
                If you didn't request this, please ignore this email.
            </p>
        ";

        var msg = new SendGridMessage()
        {
            From = from,
            Subject = subject,
            HtmlContent = htmlContent,
            PlainTextContent = $"Your password reset code: {otp}\n\nThis code expires in 10 minutes."
        };

        msg.AddTo(to);
        msg.SetClickTracking(false, false);

        try
        {
            var response = await _sendGridClient.SendEmailAsync(msg);
            return response.StatusCode == System.Net.HttpStatusCode.Accepted;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to send password reset email: {ex.Message}");
            return false;
        }
    }
}
```

---

## API Contracts

### Endpoint 1: Request Password Reset

**Request**
```http
POST /api/v1/auth/forgot-password
Content-Type: application/json

{
  "email": "user@example.com",
  "method": "email"  // or "sms"
}
```

**Success Response (200 OK)**
```json
{
  "message": "OTP sent to your email",
  "method": "email",
  "hint": "Check your email for the code",
  "expiresIn": 600
}
```

**Error Response (400 Bad Request)**
```json
{
  "error": "EMAIL_NOT_FOUND",
  "message": "This email is not registered"
}
```

**Error Response (429 Too Many Requests)**
```json
{
  "error": "RATE_LIMITED",
  "message": "Too many reset requests. Try again in 1 hour",
  "retryAfterSeconds": 3600
}
```

### Endpoint 2: Verify OTP

**Request**
```http
POST /api/v1/auth/verify-otp
Content-Type: application/json

{
  "email": "user@example.com",
  "otp": "123456"
}
```

**Success Response (200 OK)**
```json
{
  "message": "OTP verified successfully",
  "resetToken": "eyJhbGc...",
  "expiresIn": 300
}
```

**Error Response (400 Bad Request)**
```json
{
  "error": "INVALID_OTP",
  "message": "The code you entered is incorrect",
  "attemptsRemaining": 4
}
```

**Error Response (401 Unauthorized)**
```json
{
  "error": "OTP_EXPIRED",
  "message": "The code has expired. Request a new one"
}
```

### Endpoint 3: Reset Password

**Request**
```http
POST /api/v1/auth/reset-password
Content-Type: application/json

{
  "email": "user@example.com",
  "token": "eyJhbGc...",
  "newPassword": "NewPassword456!"
}
```

**Success Response (200 OK)**
```json
{
  "message": "Password reset successfully",
  "action": "redirect_to_login"
}
```

**Error Response (400 Bad Request)**
```json
{
  "error": "INVALID_TOKEN",
  "message": "The reset token is invalid or expired"
}
```

---

## Frontend Components Structure

```typescript
// src/features/auth/pages/ForgotPasswordPage.tsx
export function ForgotPasswordPage() {
  const [step, setStep] = useState<'email' | 'method' | 'otp' | 'password'>('email');
  const [email, setEmail] = useState('');
  const [method, setMethod] = useState<'email' | 'sms'>('email');
  const [otp, setOtp] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [resetToken, setResetToken] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  return (
    <div className="forgot-password-container">
      {step === 'email' && <EmailEntryForm />}
      {step === 'method' && <DeliveryMethodSelector />}
      {step === 'otp' && <OtpVerificationForm />}
      {step === 'password' && <NewPasswordForm />}
    </div>
  );
}
```

---

## Summary

This password reset feature provides:

✅ **Security**
- OTP-based verification
- Rate limiting
- Short expiration times
- Secure hashing
- Audit logging

✅ **User Experience**
- Multi-step form
- Progress indicators
- Clear error messages
- Resend options
- Email/SMS choice

✅ **Scalability**
- SendGrid integration (handles millions of emails)
- Database-backed OTP storage
- Configurable OTP settings

✅ **Compliance**
- No passwords in email
- Secure token handling
- Clear logging
- Audit trail

This is a production-grade password reset system!
