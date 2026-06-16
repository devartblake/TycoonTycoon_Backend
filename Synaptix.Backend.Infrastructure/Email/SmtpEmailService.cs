using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.Email;
using System.Net;
using System.Net.Mail;

namespace Synaptix.Backend.Infrastructure.Email;

internal sealed class SmtpEmailService(IOptions<SmtpOptions> options) : IEmailService
{
    private readonly SmtpOptions _opts = options.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        using var client = new SmtpClient(_opts.Smtp.Host, _opts.Smtp.Port)
        {
            EnableSsl = _opts.Smtp.UseSsl,
            Credentials = new NetworkCredential(_opts.Smtp.Username, _opts.Smtp.Password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_opts.FromAddress, _opts.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        await client.SendMailAsync(message, ct);
    }
}
