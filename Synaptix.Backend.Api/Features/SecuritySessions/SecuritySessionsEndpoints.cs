using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Security.Kms.Client.Abstractions;
using Synaptix.Security.Kms.Client.Exceptions;
using Synaptix.Security.Kms.Client.Models.Requests;

namespace Synaptix.Backend.Api.Features.SecuritySessions
{
    /// <summary>
    /// Public secure-channel handshake surface on the main API. The mobile client
    /// establishes its KMS secure session here (POST /api/v1/security/sessions/start,
    /// /renew, /revoke); these endpoints proxy to the KMS service via the existing
    /// IKmsSessionClient, forwarding the caller's bearer token so KMS can bind the
    /// session to the authenticated subject.
    ///
    /// Registered on the /api/v1 group, so the public paths are
    /// /api/v1/security/sessions/*. NOT marked RequireSecureChannel — this is the
    /// handshake that establishes the channel, so it must accept plain JSON.
    /// </summary>
    public static class SecuritySessionsEndpoints
    {
        private const string LogCategory =
            "Synaptix.Backend.Api.Features.SecuritySessions";

        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/security/sessions")
                .WithTags("SecuritySessions")
                .RequireAuthorization();

            g.MapPost("/start", HandleStart);
            g.MapPost("/renew", HandleRenew);
            g.MapPost("/revoke", HandleRevoke);
        }

        private static Task<IResult> HandleStart(
            [FromBody] StartSessionRequest body,
            IKmsSessionClient sessions,
            ILoggerFactory loggerFactory,
            CancellationToken ct)
            => ExecuteAsync("session/start", loggerFactory, ct, async () =>
            {
                var result = await sessions.StartAsync(
                    new StartSecureSessionRequest(
                        body.DeviceId,
                        body.ClientNonce,
                        body.ClientPublicKey,
                        body.SupportedSuites),
                    ct);

                return Results.Ok(result);
            });

        private static Task<IResult> HandleRenew(
            [FromBody] RenewSessionRequest body,
            IKmsSessionClient sessions,
            ILoggerFactory loggerFactory,
            CancellationToken ct)
            => ExecuteAsync("session/renew", loggerFactory, ct, async () =>
            {
                var result = await sessions.RenewAsync(
                    new RenewSecureSessionRequest(body.SessionId, body.DeviceId),
                    ct);

                return Results.Ok(result);
            });

        private static Task<IResult> HandleRevoke(
            [FromBody] RevokeSessionRequest body,
            IKmsSessionClient sessions,
            ILoggerFactory loggerFactory,
            CancellationToken ct)
            => ExecuteAsync("session/revoke", loggerFactory, ct, async () =>
            {
                await sessions.RevokeAsync(
                    new RevokeSecureSessionRequest(body.SessionId, body.Reason),
                    ct);

                return Results.NoContent();
            });

        /// <summary>
        /// Runs a KMS proxy call and converts failures into meaningful HTTP results.
        /// A <see cref="KmsClientException"/> carries the upstream status; anything else
        /// (KMS unreachable, request timeout, open circuit from the resilience handler,
        /// or a malformed response) is an unavailable-dependency condition and is
        /// returned as a clean 503 instead of an opaque 500 from the global handler.
        /// The mobile client already retries then degrades gracefully on a non-2xx.
        /// </summary>
        private static async Task<IResult> ExecuteAsync(
            string operation,
            ILoggerFactory loggerFactory,
            CancellationToken ct,
            Func<Task<IResult>> action)
        {
            var logger = loggerFactory.CreateLogger(LogCategory);
            try
            {
                return await action();
            }
            catch (KmsClientException ex)
            {
                logger.LogWarning(
                    ex, "KMS {Operation} returned upstream status {Status}",
                    operation, ex.StatusCode);
                return MapKmsError(ex);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Caller aborted — let the framework handle the cancellation.
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Secure {Operation} failed: the KMS dependency is unavailable",
                    operation);
                return ApiResponses.Error(
                    StatusCodes.Status503ServiceUnavailable,
                    "secure_session_unavailable",
                    "The secure channel service is temporarily unavailable. Please retry.");
            }
        }

        private static IResult MapKmsError(KmsClientException ex)
        {
            var status = ex.StatusCode switch
            {
                401 => StatusCodes.Status401Unauthorized,
                403 => StatusCodes.Status403Forbidden,
                404 => StatusCodes.Status404NotFound,
                // An upstream 5xx from KMS is a dependency failure, not a client bad
                // request — surface it as a gateway error, not a misleading 400.
                >= 500 => StatusCodes.Status502BadGateway,
                _ => StatusCodes.Status400BadRequest,
            };

            var code = status == StatusCodes.Status502BadGateway
                ? "secure_session_upstream_error"
                : "secure_session_failed";

            return ApiResponses.Error(status, code, ex.Message);
        }

        public sealed record StartSessionRequest(
            string DeviceId,
            string ClientNonce,
            string ClientPublicKey,
            IReadOnlyList<string> SupportedSuites);

        public sealed record RenewSessionRequest(Guid SessionId, string DeviceId);

        public sealed record RevokeSessionRequest(Guid SessionId, string Reason);
    }
}
