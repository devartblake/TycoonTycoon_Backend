using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Synaptix.Backend.Api.Security;

/// <summary>
/// Forwards the inbound request's <c>Authorization</c> header onto an outbound
/// HTTP call. Used so the main API can proxy a user-authenticated request to the
/// KMS secure-session surface, which derives the subject from the caller's JWT
/// (the KMS session endpoints use <c>RequireAuthorization</c>, not the service
/// token). For server-initiated calls with no active HttpContext or no inbound
/// Authorization header this is a no-op, preserving the existing
/// service-token-only behaviour of internal callers.
/// </summary>
public sealed class UserBearerForwardingHandler(IHttpContextAccessor httpContextAccessor)
    : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            var inbound = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(inbound)
                && AuthenticationHeaderValue.TryParse(inbound, out var header))
            {
                request.Headers.Authorization = header;
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
