using Microsoft.AspNetCore.Http;

namespace Synaptix.Shared.Core.Exceptions
{
    public class ConflictException(string message, Exception? innerException = null)
        : CustomException(message, StatusCodes.Status409Conflict, innerException);
}
