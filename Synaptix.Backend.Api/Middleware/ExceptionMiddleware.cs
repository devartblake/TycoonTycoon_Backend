using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace Synaptix.Backend.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
{
    public async Task Invoke(HttpContext ctx)
    {
        try { await next(ctx); }
        catch (Exception ex)
        {
            var traceId = Activity.Current?.TraceId.ToString() ?? ctx.TraceIdentifier;
            logger.LogError(ex, "Unhandled exception on {Method} {Path} traceId={TraceId}", ctx.Request.Method, ctx.Request.Path, traceId);

            ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            ctx.Response.ContentType = "application/json";

            var message = env.IsDevelopment() ? ex.Message : "An internal error occurred.";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = message, traceId }));
        }
    }
}