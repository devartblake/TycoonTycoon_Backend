namespace Synaptix.Security.Kms.Application.Payload;

public sealed class SecurePayloadException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
