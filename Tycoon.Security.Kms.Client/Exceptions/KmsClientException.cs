namespace Tycoon.Security.Kms.Client.Exceptions;

public class KmsClientException : Exception
{
    public int? StatusCode { get; }

    public KmsClientException(string message, int? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public KmsClientException(string message, Exception inner, int? statusCode = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}
