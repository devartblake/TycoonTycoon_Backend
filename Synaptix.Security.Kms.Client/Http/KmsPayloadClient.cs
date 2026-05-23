using System.Net.Http.Json;
using Synaptix.Security.Kms.Client.Abstractions;
using Synaptix.Security.Kms.Client.Exceptions;
using Synaptix.Security.Kms.Client.Models.Requests;
using Synaptix.Security.Kms.Client.Models.Responses;

namespace Synaptix.Security.Kms.Client.Http;

internal sealed class KmsPayloadClient(HttpClient http) : IKmsPayloadClient
{
    public async Task<EncryptPayloadResponse> EncryptAsync(
        EncryptPayloadRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/security/payload/encrypt", request, ct);
        await EnsureSuccessAsync(response, "payload/encrypt", ct);
        return (await response.Content.ReadFromJsonAsync<EncryptPayloadResponse>(ct))!;
    }

    public async Task<DecryptPayloadResponse> DecryptAsync(
        DecryptPayloadRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/security/payload/decrypt", request, ct);
        await EnsureSuccessAsync(response, "payload/decrypt", ct);
        return (await response.Content.ReadFromJsonAsync<DecryptPayloadResponse>(ct))!;
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
