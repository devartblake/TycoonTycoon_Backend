namespace Synaptix.Compliance.Client.Exceptions;

public sealed class ComplianceClientException(string message, int statusCode)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class ComplianceUnavailableException() : Exception("Compliance service is unavailable.");
