using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Questions;

public interface IQuestionTaxonomySidecarClient
{
    Task<QuestionTaxonomySuggestionResponse?> SuggestAsync(
        QuestionTaxonomySuggestionRequest request,
        CancellationToken ct = default);

    Task<IReadOnlyList<QuestionTaxonomySuggestionResponse?>> SuggestBatchAsync(
        IReadOnlyList<QuestionTaxonomySuggestionRequest> requests,
        CancellationToken ct = default);
}
