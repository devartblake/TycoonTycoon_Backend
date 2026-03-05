using Microsoft.AspNetCore.Http;

namespace Tycoon.Backend.Api.Contracts;

public static class ApiResponses
{
    public static IResult Error(int statusCode, string code, string message, object? details = null)
        => Results.Json(new
        {
            error = new
            {
                code,
                message,
                details = details ?? new { }
            }
        }, statusCode: statusCode);
}
