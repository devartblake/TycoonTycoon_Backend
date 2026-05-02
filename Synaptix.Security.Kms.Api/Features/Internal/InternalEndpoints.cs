using Microsoft.AspNetCore.Mvc;
using Synaptix.Security.Kms.Api.Security;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Contracts.Models;

namespace Synaptix.Security.Kms.Api.Features.Internal;

/// Internal service-to-service endpoints.
/// All routes require a valid X-Service-Token header — not exposed to public callers.
public static class InternalEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/internal/security")
            .WithTags("Internal")
            .AddEndpointFilter<ServiceTokenFilter>();

        group.MapPost("/datakey", HandleGenerateDataKey);
        group.MapPost("/encrypt", HandleEncrypt);
        group.MapPost("/decrypt", HandleDecrypt);
    }

    private static async Task<IResult> HandleGenerateDataKey(
        [FromBody] DataKeyRequest body,
        IKeyWrappingService wrapping,
        CancellationToken ct)
    {
        try
        {
            var key = await wrapping.GenerateDataKeyAsync(body.KeyName, ct);
            return Results.Ok(key);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = "datakey_failed", message = ex.Message });
        }
    }

    private static async Task<IResult> HandleEncrypt(
        [FromBody] InternalEncryptRequest body,
        ISecurePayloadProtector protector,
        CancellationToken ct)
    {
        try
        {
            var result = await protector.EncryptAsync(
                body.SessionId, body.Plaintext, body.ContentType, ct);

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = "session_invalid", message = ex.Message });
        }
    }

    private static async Task<IResult> HandleDecrypt(
        [FromBody] InternalDecryptRequest body,
        ISecurePayloadProtector protector,
        CancellationToken ct)
    {
        try
        {
            var payload = new EncryptedPayload(
                body.Ciphertext, body.Nonce, body.Mac,
                body.ContentType, body.EncryptedAtUtc);

            var (plaintext, contentType) = await protector.DecryptAsync(body.SessionId, payload, ct);
            return Results.Ok(new { plaintext, contentType });
        }
        catch (System.Security.Cryptography.AuthenticationTagMismatchException)
        {
            return Results.BadRequest(new { error = "authentication_failed", message = "Payload authentication tag mismatch." });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = "session_invalid", message = ex.Message });
        }
    }
}

public sealed record DataKeyRequest(string KeyName);

public sealed record InternalEncryptRequest(
    Guid SessionId,
    byte[] Plaintext,
    string ContentType = "application/json");

public sealed record InternalDecryptRequest(
    Guid SessionId,
    string Ciphertext,
    string Nonce,
    string Mac,
    string ContentType,
    DateTimeOffset EncryptedAtUtc);
