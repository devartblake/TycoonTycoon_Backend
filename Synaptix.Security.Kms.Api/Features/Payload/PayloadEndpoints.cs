using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Contracts.Models;

namespace Synaptix.Security.Kms.Api.Features.Payload;

public static class PayloadEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/security/payload")
            .WithTags("Payload")
            .RequireAuthorization();

        group.MapPost("/encrypt", HandleEncrypt);
        group.MapPost("/decrypt", HandleDecrypt);
    }

    private static async Task<IResult> HandleEncrypt(
        [FromBody] EncryptRequest body,
        ISecurePayloadProtector protector,
        CancellationToken ct)
    {
        try
        {
            var result = await protector.EncryptAsync(
                body.SessionId,
                body.Plaintext,
                body.ContentType,
                ct);

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = "session_invalid", message = ex.Message });
        }
    }

    private static async Task<IResult> HandleDecrypt(
        [FromBody] DecryptRequest body,
        ISecurePayloadProtector protector,
        CancellationToken ct)
    {
        try
        {
            var payload = new EncryptedPayload(
                body.Ciphertext,
                body.Nonce,
                body.Mac,
                body.ContentType,
                body.EncryptedAtUtc);

            var (plaintext, contentType) = await protector.DecryptAsync(body.SessionId, payload, ct);

            return Results.Ok(new { plaintext, contentType });
        }
        catch (AuthenticationTagMismatchException)
        {
            return Results.BadRequest(new { error = "authentication_failed", message = "Payload authentication tag mismatch." });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = "session_invalid", message = ex.Message });
        }
    }
}

public sealed record EncryptRequest(
    Guid SessionId,
    byte[] Plaintext,
    string ContentType = "application/json");

public sealed record DecryptRequest(
    Guid SessionId,
    string Ciphertext,
    string Nonce,
    string Mac,
    string ContentType,
    DateTimeOffset EncryptedAtUtc);
