using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Synaptix.Backend.Application.Email;

namespace Synaptix.Backend.Api.Services
{
    /// <summary>
    /// Service for sending emails using SendGrid
    /// Used for password reset, verification, and notifications
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public EmailService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            var apiKey = _config["SendGrid:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("[WARNING] SendGrid API key not configured");
                return;
            }

            var fromEmail = _config["SendGrid:FromEmail"] ?? "noreply@synaptixplay.com";
            var fromName = _config["SendGrid:FromName"] ?? "Trivia Tycoon";
            var payload = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[] { new { email = to } },
                        subject,
                    },
                },
                from = new { email = fromEmail, name = fromName },
                content = new[]
                {
                    new { type = "text/html", value = htmlBody },
                },
                reply_to = new { email = fromEmail },
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send")
            {
                Content = content,
            };
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException(
                    $"Failed to send email: {response.StatusCode} - {body}");
            }
        }

        /// <summary>
        /// Sends a password reset OTP email
        /// </summary>
        public async Task<bool> SendPasswordResetEmailAsync(string email, string displayName, string otp)
        {
            try
            {
                var apiKey = _config["SendGrid:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("[WARNING] SendGrid API key not configured");
                    return false;
                }

                var fromEmail = _config["SendGrid:FromEmail"] ?? "noreply@synaptixplay.com";
                var fromName = _config["SendGrid:FromName"] ?? "Trivia Tycoon";

                var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px; }}
        .header {{ text-align: center; color: #333; margin-bottom: 20px; }}
        .code-box {{
            background-color: #f0f0f0;
            border: 2px solid #007bff;
            padding: 20px;
            text-align: center;
            border-radius: 8px;
            margin: 20px 0;
        }}
        .code {{
            font-size: 36px;
            font-weight: bold;
            letter-spacing: 5px;
            color: #007bff;
            font-family: monospace;
        }}
        .footer {{ color: #999; font-size: 12px; text-align: center; margin-top: 20px; border-top: 1px solid #eee; padding-top: 10px; }}
        .warning {{ color: #d9534f; background-color: #f2dede; padding: 10px; border-radius: 4px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>Password Reset Request</h2>
        </div>

        <p>Hi {displayName},</p>

        <p>You requested to reset your Trivia Tycoon password. Use this one-time code to proceed:</p>

        <div class=""code-box"">
            <div class=""code"">{otp}</div>
        </div>

        <p><strong>Important:</strong></p>
        <ul>
            <li>This code expires in 10 minutes</li>
            <li>Do not share this code with anyone</li>
            <li>Trivia Tycoon staff will never ask for this code</li>
        </ul>

        <div class=""warning"">
            <strong>Didn't request this?</strong> Someone may have entered your email by mistake. If you didn't request a password reset, you can safely ignore this email. Your account is secure.
        </div>

        <div class=""footer"">
            <p>Sent at {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
            <p>Trivia Tycoon © 2026</p>
        </div>
    </div>
</body>
</html>";

                var plainText = $@"Password Reset Code

Hi {displayName},

You requested to reset your Trivia Tycoon password.

Use this code: {otp}

This code expires in 10 minutes.

Didn't request this? Ignore this email. Your account is secure.

Sent at {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

                var payload = new
                {
                    personalizations = new[] {
                        new {
                            to = new[] { new { email = email } },
                            subject = "Reset Your Trivia Tycoon Password"
                        }
                    },
                    from = new { email = fromEmail, name = fromName },
                    content = new[] {
                        new { type = "text/plain", value = plainText },
                        new { type = "text/html", value = htmlContent }
                    },
                    reply_to = new { email = fromEmail }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send")
                {
                    Content = content
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[INFO] Password reset email sent to {email}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[ERROR] Failed to send email: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception sending password reset email: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends a password reset confirmation email
        /// </summary>
        public async Task<bool> SendPasswordResetConfirmationEmailAsync(string email, string displayName)
        {
            try
            {
                var apiKey = _config["SendGrid:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("[WARNING] SendGrid API key not configured");
                    return false;
                }

                var fromEmail = _config["SendGrid:FromEmail"] ?? "noreply@synaptixplay.com";
                var fromName = _config["SendGrid:FromName"] ?? "Trivia Tycoon";

                var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 20px; border-radius: 8px; }}
        .header {{ text-align: center; color: #333; margin-bottom: 20px; }}
        .success {{ color: #5cb85c; background-color: #dff0d8; padding: 10px; border-radius: 4px; margin: 10px 0; }}
        .footer {{ color: #999; font-size: 12px; text-align: center; margin-top: 20px; border-top: 1px solid #eee; padding-top: 10px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h2>Password Changed Successfully</h2>
        </div>

        <p>Hi {displayName},</p>

        <div class=""success"">
            <strong>✓ Your password has been changed successfully!</strong>
        </div>

        <p>Your Trivia Tycoon account is now secured with your new password.</p>

        <p><strong>Next steps:</strong></p>
        <ul>
            <li>Sign in with your new password</li>
            <li>If you didn't change your password, secure your account immediately</li>
            <li>Enable two-factor authentication for extra security (coming soon)</li>
        </ul>

        <div class=""footer"">
            <p>Sent at {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
            <p>Trivia Tycoon © 2026</p>
        </div>
    </div>
</body>
</html>";

                var plainText = $@"Password Changed Successfully

Hi {displayName},

Your password has been changed successfully!

Your Trivia Tycoon account is now secured.

Sign in with your new password to continue.

Sent at {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

                var payload = new
                {
                    personalizations = new[] {
                        new {
                            to = new[] { new { email = email } },
                            subject = "Your Trivia Tycoon Password Has Been Changed"
                        }
                    },
                    from = new { email = fromEmail, name = fromName },
                    content = new[] {
                        new { type = "text/plain", value = plainText },
                        new { type = "text/html", value = htmlContent }
                    },
                    reply_to = new { email = fromEmail }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send")
                {
                    Content = content
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[INFO] Password confirmation email sent to {email}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[ERROR] Failed to send confirmation email: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception sending confirmation email: {ex.Message}");
                return false;
            }
        }
    }
}
