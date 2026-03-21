using System.Net.Http.Headers;

namespace Tycoon.OperatorDashboard.Services;

/// <summary>
/// DelegatingHandler that injects the current Blazor circuit's Bearer token into every
/// outgoing AdminApiClient request. By reading from the scoped BearerTokenStore rather
/// than from HttpClient.DefaultRequestHeaders, all AdminApiClient instances within the
/// same circuit automatically use the same token — regardless of how many transient
/// instances DI creates.
/// </summary>
public sealed class BearerTokenHandler(BearerTokenStore tokenStore) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(tokenStore.AccessToken))
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenStore.AccessToken);

        return base.SendAsync(request, ct);
    }
}
