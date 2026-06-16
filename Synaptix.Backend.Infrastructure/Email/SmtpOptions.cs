namespace Synaptix.Backend.Infrastructure.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; set; } = "no-reply@synaptix.gg";
    public string FromName { get; set; } = "Synaptix";

    public SmtpConnectionOptions Smtp { get; set; } = new();
}

public sealed class SmtpConnectionOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
}
