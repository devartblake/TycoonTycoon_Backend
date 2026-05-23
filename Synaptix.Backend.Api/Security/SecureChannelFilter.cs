using System.IO.Pipelines;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Security.Kms.Client.Abstractions;
using Synaptix.Security.Kms.Client.Exceptions;
using Synaptix.Security.Kms.Client.Models.Requests;
using Synaptix.Security.Kms.Client.Models.Responses;

namespace Synaptix.Backend.Api.Security;

/// <summary>
/// Middleware that requires an active KMS secure session on endpoints marked with
/// <see cref="RequireSecureChannelAttribute"/>. Incoming request body must be an
/// encrypted envelope; response body is encrypted before sending.
/// Registered via <see cref="SecureChannelExtensions.UseSecureChannel"/>.
/// </summary>
public sealed class SecureChannelMiddleware
{
    private const int MaxReplayNonceLength = 256;
    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;

    public SecureChannelMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext http)
    {
        var endpoint = http.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<RequireSecureChannelAttribute>() is null)
        {
            await _next(http);
            return;
        }

        if (IsTrustedBffPlainJsonAllowed(http, endpoint) || IsTestPlainJsonAllowed(http))
        {
            await _next(http);
            return;
        }

        var ct = http.RequestAborted;

        if (!http.Request.Headers.TryGetValue("X-Syn-Sec-Session", out var sessionIdStr)
            || !Guid.TryParse(sessionIdStr, out var sessionId))
        {
            await ApiResponses.Error(StatusCodes.Status400BadRequest,
                "secure_session_required",
                "X-Syn-Sec-Session header is required on this endpoint.").ExecuteAsync(http);
            return;
        }

        if (!TryReadSequence(http, out var sequenceNumber, out var sequenceError))
        {
            await ApiResponses.Error(StatusCodes.Status400BadRequest,
                "secure_sequence_required",
                sequenceError).ExecuteAsync(http);
            return;
        }

        if (!TryReadReplayNonce(http, out var replayNonce, out var nonceError))
        {
            await ApiResponses.Error(StatusCodes.Status400BadRequest,
                "secure_replay_nonce_required",
                nonceError).ExecuteAsync(http);
            return;
        }

        EncryptedRequestEnvelope envelope;
        try
        {
            envelope = await JsonSerializer.DeserializeAsync<EncryptedRequestEnvelope>(
                           http.Request.Body, _jsonOpts, ct)
                       ?? throw new InvalidOperationException("null body");
            if (string.IsNullOrEmpty(envelope.Ciphertext) || string.IsNullOrEmpty(envelope.Nonce)
                || string.IsNullOrEmpty(envelope.Mac) || string.IsNullOrEmpty(envelope.ContentType))
                throw new InvalidOperationException("missing required envelope fields");
        }
        catch
        {
            await ApiResponses.Error(StatusCodes.Status400BadRequest,
                "invalid_encrypted_payload",
                "Request body must be a valid encrypted envelope.").ExecuteAsync(http);
            return;
        }

        var kms = http.RequestServices.GetRequiredService<IKmsPayloadClient>();
        DecryptPayloadResponse decrypted;
        var subjectId = GetSubjectId(http);
        var requestAad = BuildAad("request", http, sessionId, sequenceNumber, subjectId, envelope.EncryptedAtUtc);
        try
        {
            decrypted = await kms.DecryptAsync(new DecryptPayloadRequest(
                sessionId,
                envelope.Ciphertext,
                envelope.Nonce,
                envelope.Mac,
                envelope.ContentType,
                envelope.EncryptedAtUtc,
                sequenceNumber,
                replayNonce,
                requestAad,
                subjectId), ct);
        }
        catch (KmsClientException ex)
        {
            var status = ex.StatusCode is 401 or 404
                ? StatusCodes.Status401Unauthorized
                : StatusCodes.Status400BadRequest;
            await ApiResponses.Error(status, "secure_session_invalid", ex.Message).ExecuteAsync(http);
            return;
        }

        // Replace request body so model binding sees decrypted plaintext, not the encrypted envelope.
        // Setting IRequestBodyPipeFeature ensures BodyReader also returns the new stream.
        http.Request.Body = new MemoryStream(decrypted.Plaintext);
        http.Request.ContentLength = decrypted.Plaintext.Length;
        http.Request.ContentType = decrypted.ContentType;
        http.Features.Set<IRequestBodyPipeFeature>(new DecryptedBodyPipeFeature(decrypted.Plaintext));

        // Capture response body to encrypt it before sending to the client
        using var buffer = new MemoryStream();
        var originalBodyFeature = http.Features.Get<IHttpResponseBodyFeature>()!;
        var captureFeature = new StreamResponseBodyFeature(buffer);
        http.Features.Set<IHttpResponseBodyFeature>(captureFeature);

        try
        {
            await _next(http);
            await captureFeature.CompleteAsync();
        }
        finally
        {
            http.Features.Set(originalBodyFeature);
        }

        var capturedStatus = http.Response.StatusCode;
        var capturedContentType = http.Response.ContentType ?? "application/json";
        var responseBytes = buffer.ToArray();

        EncryptPayloadResponse encrypted;
        try
        {
            var responseAad = BuildAad("response", http, sessionId, sequenceNumber, subjectId, envelope.EncryptedAtUtc);
            encrypted = await kms.EncryptAsync(
                new EncryptPayloadRequest(sessionId, responseBytes, capturedContentType, responseAad), ct);
        }
        catch (Exception ex)
        {
            http.Response.StatusCode = StatusCodes.Status502BadGateway;
            http.Response.ContentType = "application/json";
            await http.Response.WriteAsJsonAsync(new
            {
                error = new { code = "kms_encryption_failed", message = "Failed to encrypt response." }
            }, ct);
            http.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger<SecureChannelMiddleware>()
                .LogError(ex, "KMS response encryption failed for session {SessionId}", sessionId);
            return;
        }

        http.Response.StatusCode = capturedStatus;
        http.Response.ContentType = "application/json";
        http.Response.Headers.Remove("Content-Length");
        await http.Response.WriteAsJsonAsync(encrypted, _jsonOpts, ct);
    }

    private static bool TryReadSequence(HttpContext http, out long sequenceNumber, out string error)
    {
        sequenceNumber = 0;
        if (!http.Request.Headers.TryGetValue("X-Syn-Sec-Seq", out var raw)
            || !long.TryParse(raw, out sequenceNumber)
            || sequenceNumber <= 0)
        {
            error = "X-Syn-Sec-Seq header is required and must be a positive integer.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryReadReplayNonce(HttpContext http, out string replayNonce, out string error)
    {
        replayNonce = string.Empty;
        if (!http.Request.Headers.TryGetValue("X-Syn-Sec-Nonce", out var raw)
            || string.IsNullOrWhiteSpace(raw))
        {
            error = "X-Syn-Sec-Nonce header is required on this endpoint.";
            return false;
        }

        replayNonce = raw.ToString();
        if (replayNonce.Length > MaxReplayNonceLength)
        {
            error = "X-Syn-Sec-Nonce header is too long.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static string BuildAad(
        string direction,
        HttpContext http,
        Guid sessionId,
        long sequenceNumber,
        string? subjectId,
        DateTimeOffset encryptedAtUtc)
    {
        var target = $"{http.Request.PathBase}{http.Request.Path}{http.Request.QueryString}";
        return string.Join('|',
            "syn-sec-v1",
            direction,
            http.Request.Method.ToUpperInvariant(),
            target,
            sessionId.ToString("N"),
            sequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
            subjectId ?? string.Empty,
            encryptedAtUtc.ToUniversalTime().ToString("O"));
    }

    private static string? GetSubjectId(HttpContext http)
        => http.User.FindFirst("sub")?.Value
           ?? http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
           ?? http.User.Identity?.Name;

    private static bool IsTrustedBffPlainJsonAllowed(HttpContext http, Endpoint endpoint)
    {
        if (endpoint.Metadata.GetMetadata<AllowTrustedBffPlainJsonAttribute>() is null)
            return false;
        if (http.Request.Headers.ContainsKey("X-Syn-Sec-Session"))
            return false;
        var cfg = http.RequestServices.GetRequiredService<IConfiguration>();
        if (!cfg.GetValue("AdminAuth:AllowTrustedBffPlainJson", false))
            return false;
        var logger = http.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("TrustedBffPlainJson");
        return AdminOpsKeyMiddleware.ValidateOpsKey(http, cfg, logger) is null;
    }

    private static bool IsTestPlainJsonAllowed(HttpContext http)
    {
        var cfg = http.RequestServices.GetRequiredService<IConfiguration>();
        return cfg.GetValue("Testing:UseInMemoryDb", false)
            && cfg.GetValue("SecureChannel:AllowPlainJsonInTests", false)
            && !http.Request.Headers.ContainsKey("X-Syn-Sec-Session");
    }
}

internal sealed record EncryptedRequestEnvelope(
    string Ciphertext,
    string Nonce,
    string Mac,
    string ContentType,
    DateTimeOffset EncryptedAtUtc);

internal sealed class DecryptedBodyPipeFeature : IRequestBodyPipeFeature
{
    public PipeReader Reader { get; }

    internal DecryptedBodyPipeFeature(byte[] bytes)
        => Reader = PipeReader.Create(new MemoryStream(bytes));
}
