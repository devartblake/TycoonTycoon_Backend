using Microsoft.Extensions.Logging;
using Synaptix.Backend.Application.Email;

namespace Synaptix.Backend.Infrastructure.Email;

// Used when SMTP is not configured. Logs the email instead of sending.
internal sealed class NullEmailService(ILogger<NullEmailService> logger) : IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        logger.LogInformation("[NullEmailService] To={To} Subject={Subject} Body={Body}",
            to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
