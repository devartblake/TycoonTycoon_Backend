using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Security.Kms.Client.Abstractions;
using Tycoon.Security.Kms.Client.Exceptions;
using Tycoon.Security.Kms.Client.Models.Requests;
using Tycoon.Security.Kms.Client.Models.Responses;

namespace Tycoon.Backend.Api.Security;

/// <summary>
/// Endpoint filter that requires an active KMS secure session.
/// Incoming request body must be an encrypted envelope; response body is encrypted before sending.
/// Apply via <see cref="SecureChannelExtensions.RequireSecureChannel"/>.
/// </summary>
public sealed class SecureChannelFilter : IEndpointFilter
{
    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web);

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var ct = http.RequestAborted;

        if (IsTrustedBffPlainJsonAllowed(http))
        {
            return await next(context);
        }

        if (!http.Request.Headers.TryGetValue("X-Syn-Sec-Session", out var sessionIdStr)
            || !Guid.TryParse(sessionIdStr, out var sessionId))
        {
            return ApiResponses.Error(StatusCodes.Status400BadRequest,
                "secure_session_required",
                "X-Syn-Sec-Session header is required on this endpoint.");
        }

        EncryptedRequestEnvelope envelope;
        try
        {
            envelope = await JsonSerializer.DeserializeAsync<EncryptedRequestEnvelope>(
                           http.Request.Body, _jsonOpts, ct)
                       ?? throw new InvalidOperationException("null body");
        }
        catch
        {
            return ApiResponses.Error(StatusCodes.Status400BadRequest,
                "invalid_encrypted_payload",
                "Request body must be a JSON encrypted payload envelope (ciphertext, nonce, mac, contentType, encryptedAtUtc).");
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
            return ApiResponses.Error(status, "secure_session_invalid", ex.Message);
        }

        // Replace request body with decrypted plaintext so the handler sees plain JSON
        http.Request.Body = new MemoryStream(decrypted.Plaintext);
        http.Request.ContentLength = decrypted.Plaintext.Length;
        http.Request.ContentType = decrypted.ContentType;

        var handlerResult = await next(context);

        return new EncryptedResponseResult(sessionId, handlerResult, kms);
    }

    private static bool IsTrustedBffPlainJsonAllowed(HttpContext http)
    {
        var endpoint = http.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<AllowTrustedBffPlainJsonAttribute>() is null)
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
}

internal sealed record EncryptedRequestEnvelope(
    string Ciphertext,
    string Nonce,
    string Mac,
    string ContentType,
    DateTimeOffset EncryptedAtUtc);

/// <summary>
/// Captures the handler's response bytes, encrypts them via KMS, and writes the
/// encrypted envelope as the actual HTTP response body.
/// </summary>
internal sealed class EncryptedResponseResult : IResult
{
    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web);

    private readonly Guid _sessionId;
    private readonly object? _inner;
    private readonly IKmsPayloadClient _kms;

    internal EncryptedResponseResult(Guid sessionId, object? inner, IKmsPayloadClient kms)
    {
        _sessionId = sessionId;
        _inner = inner;
        _kms = kms;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var ct = httpContext.RequestAborted;

        // Capture the inner result's bytes by temporarily redirecting the response body
        using var buffer = new MemoryStream();
        var originalBodyFeature = httpContext.Features.Get<IHttpResponseBodyFeature>()!;
        var captureFeature = new StreamResponseBodyFeature(buffer);
        httpContext.Features.Set<IHttpResponseBodyFeature>(captureFeature);

        try
        {
            switch (_inner)
            {
                case IResult ir:
                    await ir.ExecuteAsync(httpContext);
                    break;
                case null:
                    httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
                    break;
                default:
                    await httpContext.Response.WriteAsJsonAsync(_inner, _jsonOpts, ct);
                    break;
            }
            await captureFeature.CompleteAsync();
        }
        finally
        {
            httpContext.Features.Set(originalBodyFeature);
        }

        var capturedStatus = httpContext.Response.StatusCode;
        var capturedContentType = httpContext.Response.ContentType ?? "application/json";
        var responseBytes = buffer.ToArray();

        EncryptPayloadResponse encrypted;
        try
        {
            encrypted = await _kms.EncryptAsync(
                new EncryptPayloadRequest(_sessionId, responseBytes, capturedContentType), ct);
        }
        catch (Exception ex)
        {
            httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsJsonAsync(new
            {
                error = new { code = "kms_encryption_failed", message = "Failed to encrypt response." }
            }, ct);
            httpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<SecureChannelFilter>()
                .LogError(ex, "KMS response encryption failed for session {SessionId}", _sessionId);
            return;
        }

        // Write encrypted payload with the same HTTP status as the inner result
        httpContext.Response.StatusCode = capturedStatus;
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.Headers.Remove("Content-Length");
        await httpContext.Response.WriteAsJsonAsync(encrypted, _jsonOpts, ct);
    }
}
