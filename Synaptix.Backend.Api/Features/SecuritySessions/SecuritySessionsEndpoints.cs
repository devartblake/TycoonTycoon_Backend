using Microsoft.AspNetCore.Mvc;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Security.Kms.Client.Abstractions;
using Synaptix.Security.Kms.Client.Exceptions;
using Synaptix.Security.Kms.Client.Models.Requests;

namespace Synaptix.Backend.Api.Features.Security
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
        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/security/sessions")
                .WithTags("SecuritySessions")
                .RequireAuthorization();

            g.MapPost("/start", HandleStart);
            g.MapPost("/renew", HandleRenew);
            g.MapPost("/revoke", HandleRevoke);
        }

        private static async Task<IResult> HandleStart(
            [FromBody] StartSessionRequest body,
            IKmsSessionClient sessions,
            CancellationToken ct)
        {
            try
            {
                var result = await sessions.StartAsync(
                    new StartSecureSessionRequest(
                        body.DeviceId,
                        body.ClientNonce,
                        body.ClientPublicKey,
                        body.SupportedSuites),
                    ct);

                return Results.Ok(result);
            }
            catch (KmsClientException ex)
            {
                return MapKmsError(ex);
            }
        }

        private static async Task<IResult> HandleRenew(
            [FromBody] RenewSessionRequest body,
            IKmsSessionClient sessions,
            CancellationToken ct)
        {
            try
            {
                var result = await sessions.RenewAsync(
                    new RenewSecureSessionRequest(body.SessionId, body.DeviceId),
                    ct);

                return Results.Ok(result);
            }
            catch (KmsClientException ex)
            {
                return MapKmsError(ex);
            }
        }

        private static async Task<IResult> HandleRevoke(
            [FromBody] RevokeSessionRequest body,
            IKmsSessionClient sessions,
            CancellationToken ct)
        {
            try
            {
                await sessions.RevokeAsync(
                    new RevokeSecureSessionRequest(body.SessionId, body.Reason),
                    ct);

                return Results.NoContent();
            }
            catch (KmsClientException ex)
            {
                return MapKmsError(ex);
            }
        }

        private static IResult MapKmsError(KmsClientException ex)
        {
            var status = ex.StatusCode switch
            {
                401 => StatusCodes.Status401Unauthorized,
                403 => StatusCodes.Status403Forbidden,
                404 => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status400BadRequest,
            };
            return ApiResponses.Error(status, "secure_session_failed", ex.Message);
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
