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
        var model = await response.Content.ReadFromJsonAsync<ConsentStatusResponse>(ct);
        return model ?? throw new ComplianceClientException(
            "Compliance get-consent-status returned an empty response body.",
            (int)response.StatusCode);

    public async Task RecordAuditEventAsync(RecordAuditEventRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/internal/compliance/audit", request, ct);
        await EnsureSuccessAsync(response, "record-audit", ct);
    }

    public async Task<InitiateConsentResponse> InitiateParentalConsentAsync(
        InitiateParentalConsentRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/internal/compliance/parental-consent/initiate", request, ct);
        await EnsureSuccessAsync(response, "initiate-parental-consent", ct);
        return (await response.Content.ReadFromJsonAsync<InitiateConsentResponse>(ct))!;
    }

    public async Task<IReadOnlyList<PendingPrivacyRequestItem>> GetPendingPrivacyRequestsAsync(
        int limit, CancellationToken ct)
    {
        var response = await http.GetAsync($"/internal/compliance/privacy-requests/pending?limit={limit}", ct);
        await EnsureSuccessAsync(response, "get-pending-privacy-requests", ct);
        return (await response.Content.ReadFromJsonAsync<List<PendingPrivacyRequestItem>>(ct))!;
    }

    public async Task CompletePrivacyRequestAsync(Guid requestId, string status, string? notes, CancellationToken ct)
    {
        var response = await http.PatchAsJsonAsync(
            $"/internal/compliance/privacy-requests/{requestId}",
            new { Status = status, Notes = notes },
            ct);
        await EnsureSuccessAsync(response, "complete-privacy-request", ct);
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
