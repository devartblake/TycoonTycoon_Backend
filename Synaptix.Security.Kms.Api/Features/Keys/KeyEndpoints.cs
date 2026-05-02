using Microsoft.AspNetCore.Mvc;
using Synaptix.Security.Kms.Api.Security;
using Synaptix.Security.Kms.Application.Keys;

namespace Synaptix.Security.Kms.Api.Features.Keys;

public static class KeyEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/security/keys")
            .WithTags("Keys")
            .AddEndpointFilter<ServiceTokenFilter>();

        group.MapPost("/rotate", HandleRotate);
    }

    private static async Task<IResult> HandleRotate(
        [FromBody] RotateRequest body,
        KeyRotationService rotationService,
        CancellationToken ct)
    {
        try
        {
            var result = await rotationService.RotateAsync(body.KeyName, ct);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = "rotation_failed", message = ex.Message });
        }
    }
}

public sealed record RotateRequest(string KeyName);
