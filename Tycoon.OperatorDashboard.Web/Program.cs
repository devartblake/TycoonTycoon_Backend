using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

var backendBaseUrl = builder.Configuration["Backend:BaseUrl"] ?? "http://localhost:8080";

builder.Services.AddHttpClient("tycoon-api", client =>
{
    client.BaseAddress = new Uri(backendBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated is not true
        && context.Request.Headers.TryGetValue("X-Operator-User", out var headerUser)
        && !string.IsNullOrWhiteSpace(headerUser))
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, headerUser.ToString()) };
        if (context.Request.Headers.TryGetValue("X-Operator-Permissions", out var headerPerms))
        {
            foreach (var permission in headerPerms.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim("permission", permission));
            }
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "operator-header");
        context.User = new ClaimsPrincipal(identity);
    }

    await next();
});

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", backend = backendBaseUrl }));

var proxyMethods = new[] { "GET", "POST", "PUT", "PATCH", "DELETE" };

// Initial typed Wave A BFF endpoints.
app.MapGet("/api/dashboard/overview",
    (HttpContext context, IHttpClientFactory httpClientFactory) =>
        ProxyToBackend(context, httpClientFactory, $"/admin/dashboard{context.Request.QueryString}"));

app.MapGet("/api/audit-log",
    (HttpContext context, IHttpClientFactory httpClientFactory) =>
        ProxyToBackend(context, httpClientFactory, $"/admin/audit-log{context.Request.QueryString}"));

app.MapGet("/api/users",
    (HttpContext context, IHttpClientFactory httpClientFactory) =>
        ProxyToBackend(context, httpClientFactory, $"/admin/users{context.Request.QueryString}"));

app.MapMethods("/api/admin/{**path}", proxyMethods,
    (HttpContext context, IHttpClientFactory httpClientFactory, string? path) =>
        ProxyToBackend(context, httpClientFactory, $"/admin/{path}{context.Request.QueryString}"));

// Initial domain-specific BFF groups for Wave A migration.
app.MapMethods("/api/dashboard/{**path}", proxyMethods,
    (HttpContext context, IHttpClientFactory httpClientFactory, string? path) =>
        ProxyToBackend(context, httpClientFactory, $"/admin/dashboard/{path}{context.Request.QueryString}"));

app.MapMethods("/api/audit-log/{**path}", proxyMethods,
    (HttpContext context, IHttpClientFactory httpClientFactory, string? path) =>
        ProxyToBackend(context, httpClientFactory, $"/admin/audit-log/{path}{context.Request.QueryString}"));

app.MapMethods("/api/users/{**path}", proxyMethods,
    (HttpContext context, IHttpClientFactory httpClientFactory, string? path) =>
        ProxyToBackend(context, httpClientFactory, $"/admin/users/{path}{context.Request.QueryString}"));

app.MapGet("/api/me", (HttpContext context) =>
{
    var permissions = context.Request.Headers.TryGetValue("X-Operator-Permissions", out var perms)
        ? perms.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        : [];

    if (context.User?.Identity?.IsAuthenticated is true)
    {
        return Results.Ok(new
        {
            authenticated = true,
            name = context.User.Identity.Name ?? "unknown",
            permissions
        });
    }

    return Results.Ok(new { authenticated = false, name = "anonymous", permissions });
});

app.Run();

static async Task<IResult> ProxyToBackend(
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    string backendPathAndQuery)
{
    var client = httpClientFactory.CreateClient("tycoon-api");
    var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), backendPathAndQuery);

    if (context.Request.ContentLength is > 0)
    {
        request.Content = new StreamContent(context.Request.Body);

        if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
        {
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
        }
    }

    if (context.Request.Headers.TryGetValue("Authorization", out var auth) && !string.IsNullOrWhiteSpace(auth))
    {
        request.Headers.TryAddWithoutValidation("Authorization", auth.ToString());
    }

    if (context.User?.Identity?.IsAuthenticated is true)
    {
        request.Headers.TryAddWithoutValidation("X-Operator-User", context.User.Identity?.Name ?? "unknown");
    }

    try
    {
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        var responseBytes = await response.Content.ReadAsByteArrayAsync(context.RequestAborted);

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        var contentText = System.Text.Encoding.UTF8.GetString(responseBytes);
        var isJson = contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);

        if ((int)response.StatusCode >= 400 && !isJson)
        {
            return TypedResults.Json(new
            {
                error = new
                {
                    code = "BFF_UPSTREAM_ERROR",
                    message = "Upstream service returned a non-JSON error payload.",
                    details = new
                    {
                        upstreamStatus = (int)response.StatusCode,
                        upstreamContentType = contentType,
                        body = string.IsNullOrWhiteSpace(contentText) ? null : contentText
                    }
                }
            }, statusCode: (int)response.StatusCode);
        }

        return TypedResults.Content(contentText, contentType, statusCode: (int)response.StatusCode);
    }
    catch (TaskCanceledException) when (!context.RequestAborted.IsCancellationRequested)
    {
        return TypedResults.Json(new
        {
            error = new
            {
                code = "BFF_UPSTREAM_TIMEOUT",
                message = "Timed out while waiting for backend response."
            }
        }, statusCode: StatusCodes.Status504GatewayTimeout);
    }
    catch (HttpRequestException ex)
    {
        return TypedResults.Json(new
        {
            error = new
            {
                code = "BFF_UPSTREAM_UNREACHABLE",
                message = "Unable to reach backend service.",
                details = ex.Message
            }
        }, statusCode: StatusCodes.Status502BadGateway);
    }
}
