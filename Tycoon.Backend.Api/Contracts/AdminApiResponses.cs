using Microsoft.AspNetCore.Http;

namespace Tycoon.Backend.Api.Contracts;

public static class AdminApiResponses
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

    public static object Page<T>(IReadOnlyList<T> items, int page, int pageSize, int totalItems)
    {
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
        return new
        {
            items,
            page,
            pageSize,
            totalItems,
            totalPages
        };
    }
}
