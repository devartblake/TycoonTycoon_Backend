using Microsoft.AspNetCore.Mvc;
using Synaptix.Security.Kms.Api.Security;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Application.Payload;
using Synaptix.Security.Kms.Application.Sessions;
using Synaptix.Security.Kms.Contracts.Models;
using Synaptix.Security.Kms.Contracts.Suites;
using System.Security.Cryptography;

namespace Synaptix.Security.Kms.Api.Features.Internal;

/// Internal service-to-service endpoints.
/// All routes require a valid X-Service-Token header and are not exposed to public callers.
public static class InternalEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/internal/security")
            .WithTags("Internal")
            .AddEndpointFilter<ServiceTokenFilter>();

        group.MapPost("/datakey", HandleGenerateDataKey);
        group.MapPost("/sessions/start", HandleStartSession);
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

    public static async Task<IResult> HandleStartSession(
        [FromBody] InternalStartSessionRequest body,
        ISessionStore sessions,
        CancellationToken ct)
    {
        var subjectId = string.IsNullOrWhiteSpace(body.SubjectId) ? "trusted-service" : body.SubjectId.Trim();
        var deviceId = string.IsNullOrWhiteSpace(body.DeviceId) ? "service-bff" : body.DeviceId.Trim();
        var sessionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(30);
        var suite = body.SupportedSuites?.Contains(SecureSuites.ClassicalV1) == true
            ? SecureSuites.ClassicalV1
            : SecureSuites.ClassicalV1;

        var sharedPayloadKey = RandomNumberGenerator.GetBytes(32);
        var session = new SecureSession(
            sessionId,
            subjectId,
            deviceId,
            "syn-sec-v1",
            suite,
            sharedPayloadKey,
            sharedPayloadKey,
            now,
            expiresAt,
            0L);

        await sessions.SaveAsync(session, ct);

        return Results.Ok(new StartSessionResult(
            sessionId,
            "syn-sec-v1",
            suite,
            "internal-service-session",
            "internal-service-session",
            expiresAt,
            "internal-service-session"));
    }

    private static async Task<IResult> HandleEncrypt(
        [FromBody] InternalEncryptRequest body,
        ISecurePayloadProtector protector,
        CancellationToken ct)
    {
        try
        {
            var result = await protector.EncryptAsync(
                body.SessionId,
                body.Plaintext,
                body.ContentType,
                ct,
                body.Aad,
                body.Direction);

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

            var (plaintext, contentType) = await protector.DecryptAsync(
                body.SessionId,
                payload,
                ct,
                body.SequenceNumber,
                body.ReplayNonce,
                body.Aad,
                body.SubjectId,
                body.Direction,
                body.EnforceReplay ?? !string.Equals(body.Direction, "server-to-client", StringComparison.OrdinalIgnoreCase));
            return Results.Ok(new { plaintext, contentType });
        }
        catch (SecurePayloadException ex) when (ex.Code == "direction_invalid")
        {
            return Results.BadRequest(new { error = ex.Code, message = ex.Message });
        }
        catch (System.Security.Cryptography.AuthenticationTagMismatchException)
        {
            return Results.BadRequest(new { error = "authentication_failed", message = "Payload authentication tag mismatch." });
        }
        catch (SecurePayloadException ex)
        {
            return Results.BadRequest(new { error = ex.Code, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = "session_invalid", message = ex.Message });
        }
    }
}

public sealed record DataKeyRequest(string KeyName);

public sealed record InternalStartSessionRequest(
    string? SubjectId,
    string? DeviceId,
    IReadOnlyList<string>? SupportedSuites);

public sealed record InternalEncryptRequest(
    Guid SessionId,
    byte[] Plaintext,
    string ContentType = "application/json",
    string? Aad = null,
    string Direction = "server-to-client");

public sealed record InternalDecryptRequest(
    Guid SessionId,
    string Ciphertext,
    string Nonce,
    string Mac,
    string ContentType,
    DateTimeOffset EncryptedAtUtc,
    long? SequenceNumber = null,
    string? ReplayNonce = null,
    string? Aad = null,
    string? SubjectId = null,
    string Direction = "client-to-server",
    bool? EnforceReplay = null);
