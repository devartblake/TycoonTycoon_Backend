namespace Synaptix.Security.Kms.Client.Exceptions;

/// Thrown when KmsClientOptions.Required is true and the KMS API cannot be reached.
public sealed class KmsUnavailableException : KmsClientException
{
    public KmsUnavailableException(string message, Exception? inner = null)
        : base(message, inner) { }
}
