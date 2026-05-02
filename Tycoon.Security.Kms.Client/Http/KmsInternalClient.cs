using System.Net.Http.Json;
using Tycoon.Security.Kms.Client.Abstractions;
using Tycoon.Security.Kms.Client.Exceptions;
using Tycoon.Security.Kms.Client.Models.Requests;
using Tycoon.Security.Kms.Client.Models.Responses;

namespace Tycoon.Security.Kms.Client.Http;

internal sealed class KmsInternalClient(HttpClient http) : IKmsInternalClient
{
    public async Task<GenerateDataKeyResponse> GenerateDataKeyAsync(
        GenerateDataKeyRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/internal/security/datakey", request, ct);
        await EnsureSuccessAsync(response, "internal/datakey", ct);
        return (await response.Content.ReadFromJsonAsync<GenerateDataKeyResponse>(ct))!;
    }

    public async Task<EncryptPayloadResponse> EncryptAsync(
        EncryptPayloadRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/internal/security/encrypt", request, ct);
        await EnsureSuccessAsync(response, "internal/encrypt", ct);
        return (await response.Content.ReadFromJsonAsync<EncryptPayloadResponse>(ct))!;
    }

    public async Task<DecryptPayloadResponse> DecryptAsync(
        DecryptPayloadRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/internal/security/decrypt", request, ct);
        await EnsureSuccessAsync(response, "internal/decrypt", ct);
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
