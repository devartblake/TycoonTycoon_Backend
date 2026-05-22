using System.Net.Http.Json;
using Synaptix.Security.Kms.Client.Abstractions;
using Synaptix.Security.Kms.Client.Exceptions;
using Synaptix.Security.Kms.Client.Models.Requests;
using Synaptix.Security.Kms.Client.Models.Responses;

namespace Synaptix.Security.Kms.Client.Http;

internal sealed class KmsKeyClient(HttpClient http) : IKmsKeyClient
{
    public async Task<RotateKeyResponse> RotateAsync(
        RotateKeyRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/security/keys/rotate", request, ct);
        await EnsureSuccessAsync(response, "keys/rotate", ct);
        return (await response.Content.ReadFromJsonAsync<RotateKeyResponse>(ct))!;
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
