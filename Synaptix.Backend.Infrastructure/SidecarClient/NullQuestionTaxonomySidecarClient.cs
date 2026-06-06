using Synaptix.Backend.Application.Questions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Infrastructure.SidecarClient;

public sealed class NullQuestionTaxonomySidecarClient : IQuestionTaxonomySidecarClient
{
    public Task<QuestionTaxonomySuggestionResponse?> SuggestAsync(
        QuestionTaxonomySuggestionRequest request,
        CancellationToken ct = default) =>
        Task.FromResult<QuestionTaxonomySuggestionResponse?>(null);

    public Task<IReadOnlyList<QuestionTaxonomySuggestionResponse?>> SuggestBatchAsync(
        IReadOnlyList<QuestionTaxonomySuggestionRequest> requests,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<QuestionTaxonomySuggestionResponse?>>(
            requests.Select(_ => (QuestionTaxonomySuggestionResponse?)null).ToList());
}
