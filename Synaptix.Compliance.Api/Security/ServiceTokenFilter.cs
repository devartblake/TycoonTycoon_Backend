namespace Synaptix.Compliance.Api.Security;

public sealed class ServiceTokenFilter(IConfiguration configuration) : IEndpointFilter
{
    private readonly string? _expected = configuration["ComplianceApi:ServiceToken"];

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (string.IsNullOrWhiteSpace(_expected))
            return await next(context);

        if (!context.HttpContext.Request.Headers.TryGetValue("X-Service-Token", out var value)
            || value != _expected)
        {
            return Results.Json(
                new { error = "service_auth_required", message = "Valid X-Service-Token header is required." },
                statusCode: 401);
        }

        return await next(context);
    }
}
