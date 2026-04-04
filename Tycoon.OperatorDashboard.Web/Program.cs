using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

var backendBaseUrl = builder.Configuration["Backend:BaseUrl"] ?? "http://localhost:8080";

builder.Services.AddHttpClient("tycoon-api", client =>
{
    client.BaseAddress = new Uri(backendBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", backend = backendBaseUrl }));

app.MapMethods("/api/admin/{**path}", ["GET", "POST", "PUT", "PATCH", "DELETE"],
    async Task<IResult> (HttpContext context, IHttpClientFactory httpClientFactory, string? path) =>
    {
        var client = httpClientFactory.CreateClient("tycoon-api");
        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method),
            $"/admin/{path}{context.Request.QueryString}");

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

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        var responseBytes = await response.Content.ReadAsByteArrayAsync(context.RequestAborted);

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        return Results.Content(System.Text.Encoding.UTF8.GetString(responseBytes), contentType, statusCode: (int)response.StatusCode);
    });

app.Run();
