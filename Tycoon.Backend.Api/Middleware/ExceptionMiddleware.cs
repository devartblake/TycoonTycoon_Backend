using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;

namespace Tycoon.Backend.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext ctx)
    {
        try { await next(ctx); }
        catch (Exception ex)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        }
    }
}