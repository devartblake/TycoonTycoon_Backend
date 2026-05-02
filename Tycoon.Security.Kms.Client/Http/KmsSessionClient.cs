using System.Net.Http.Json;
using Tycoon.Security.Kms.Client.Abstractions;
using Tycoon.Security.Kms.Client.Exceptions;
using Tycoon.Security.Kms.Client.Models.Requests;
using Tycoon.Security.Kms.Client.Models.Responses;

namespace Tycoon.Security.Kms.Client.Http;

internal sealed class KmsSessionClient(HttpClient http) : IKmsSessionClient
{
    public async Task<StartSecureSessionResponse> StartAsync(
        StartSecureSessionRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/security/sessions/start", request, ct);
        await EnsureSuccessAsync(response, "session/start", ct);
        return (await response.Content.ReadFromJsonAsync<StartSecureSessionResponse>(ct))!;
    }

    public async Task<RenewSecureSessionResponse> RenewAsync(
        RenewSecureSessionRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/security/sessions/renew", request, ct);
        await EnsureSuccessAsync(response, "session/renew", ct);
        return (await response.Content.ReadFromJsonAsync<RenewSecureSessionResponse>(ct))!;
    }

    public async Task RevokeAsync(RevokeSecureSessionRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/security/sessions/revoke", request, ct);
        await EnsureSuccessAsync(response, "session/revoke", ct);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response, string operation, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new KmsClientException(
            $"KMS {operation} failed with {(int)response.StatusCode}: {body}",
            (int)response.StatusCode);
    }
}
