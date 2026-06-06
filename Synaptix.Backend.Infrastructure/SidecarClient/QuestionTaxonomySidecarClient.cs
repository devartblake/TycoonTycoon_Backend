using System.Net.Http.Json;
using System.Text.Json;
using Synaptix.Backend.Application.Questions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Infrastructure.SidecarClient;

public sealed class QuestionTaxonomySidecarClient(HttpClient http) : IQuestionTaxonomySidecarClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<QuestionTaxonomySuggestionResponse?> SuggestAsync(
        QuestionTaxonomySuggestionRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var response = await http.PostAsJsonAsync("/ml/question-taxonomy", request, Json, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<QuestionTaxonomySuggestionResponse>(Json, ct);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<QuestionTaxonomySuggestionResponse?>> SuggestBatchAsync(
        IReadOnlyList<QuestionTaxonomySuggestionRequest> requests,
        CancellationToken ct = default)
    {
        if (requests.Count == 0) return [];

        try
        {
            var response = await http.PostAsJsonAsync("/ml/question-taxonomy/batch", new QuestionTaxonomyBatchSuggestionRequest(requests), Json, ct);
            if (!response.IsSuccessStatusCode)
                return requests.Select(_ => (QuestionTaxonomySuggestionResponse?)null).ToList();

            var payload = await response.Content.ReadFromJsonAsync<QuestionTaxonomyBatchSuggestionResponse>(Json, ct);
            return payload?.Suggestions ?? requests.Select(_ => (QuestionTaxonomySuggestionResponse?)null).ToList();
        }
        catch
        {
            return requests.Select(_ => (QuestionTaxonomySuggestionResponse?)null).ToList();
        }
    }
}
