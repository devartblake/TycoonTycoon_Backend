namespace Tycoon.Shared.Core.Exceptions
{
    /// <summary>
    /// Exception type for validation exceptions.
    /// </summary>
    public class ValidationException(string message, Exception? innerException = null, params string[] errors)
        : BadRequestException(message, innerException, errors);
}
