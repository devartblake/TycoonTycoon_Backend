using System.IO.Pipelines;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Security.Kms.Client.Abstractions;
using Tycoon.Security.Kms.Client.Exceptions;
using Tycoon.Security.Kms.Client.Models.Requests;
using Tycoon.Security.Kms.Client.Models.Responses;

namespace Tycoon.Backend.Api.Security;

/// <summary>
/// Middleware that requires an active KMS secure session on endpoints marked with
/// <see cref="RequireSecureChannelAttribute"/>. Incoming request body must be an
/// encrypted envelope; response body is encrypted before sending.
/// Registered via <see cref="SecureChannelExtensions.UseSecureChannel"/>.
/// </summary>
public sealed class SecureChannelMiddleware
{
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
        try
        {
            decrypted = await kms.DecryptAsync(new DecryptPayloadRequest(
                sessionId,
                envelope.Ciphertext,
                envelope.Nonce,
                envelope.Mac,
                envelope.ContentType,
                envelope.EncryptedAtUtc), ct);
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
            encrypted = await kms.EncryptAsync(
                new EncryptPayloadRequest(sessionId, responseBytes, capturedContentType), ct);
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
