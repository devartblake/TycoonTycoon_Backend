using Microsoft.AspNetCore.Http;

namespace Tycoon.Shared.Core.Exceptions
{
    /// <summary>
    /// Exception type for not found exceptions.
    /// </summary>
    public class NotFoundException(string message, Exception? innerException = null)
        : CustomException(message, StatusCodes.Status404NotFound, innerException);
}
