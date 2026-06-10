using System.Net.Http.Json;
using Synaptix.Compliance.Client.Abstractions;
using Synaptix.Compliance.Client.Exceptions;
using Synaptix.Compliance.Client.Models.Requests;
using Synaptix.Compliance.Client.Models.Responses;

namespace Synaptix.Compliance.Client.Http;

internal sealed class ComplianceClient(HttpClient http) : IComplianceClient
{
    public async Task<UserRestrictionsResponse> GetUserRestrictionsAsync(Guid userId, CancellationToken ct)
    {
        var response = await http.GetAsync($"/internal/compliance/users/{userId}/restrictions", ct);
        await EnsureSuccessAsync(response, "get-restrictions", ct);
        var model = await response.Content.ReadFromJsonAsync<UserRestrictionsResponse>(ct);
        return model ?? throw new ComplianceClientException(
            "Compliance get-restrictions returned an empty response body.",
            (int)response.StatusCode);

    public async Task<ConsentStatusResponse> GetConsentStatusAsync(Guid userId, CancellationToken ct)
    {
        var response = await http.GetAsync($"/internal/compliance/users/{userId}/consent-status", ct);
        await EnsureSuccessAsync(response, "get-consent-status", ct);
        return (await response.Content.ReadFromJsonAsync<ConsentStatusResponse>(ct))!;
    }

    public async Task RecordAuditEventAsync(RecordAuditEventRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/internal/compliance/audit", request, ct);
        await EnsureSuccessAsync(response, "record-audit", ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new ComplianceClientException(
            $"Compliance {operation} failed with {(int)response.StatusCode}: {body}",
            (int)response.StatusCode);
    }
}
