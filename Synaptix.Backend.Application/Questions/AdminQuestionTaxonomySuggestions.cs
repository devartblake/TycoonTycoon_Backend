using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Questions;

public sealed record AdminSuggestQuestionTaxonomy(Guid QuestionId) : IRequest<QuestionTaxonomyStoredSuggestionDto?>;
public sealed record AdminListQuestionTaxonomySuggestions(Guid QuestionId) : IRequest<IReadOnlyList<QuestionTaxonomyStoredSuggestionDto>>;
public sealed record AdminApplyQuestionTaxonomySuggestion(Guid QuestionId, ApplyQuestionTaxonomySuggestionRequest Request) : IRequest<QuestionDto?>;

public sealed class AdminSuggestQuestionTaxonomyHandler(
    IAppDb db,
    IQuestionTaxonomySidecarClient sidecar)
    : IRequestHandler<AdminSuggestQuestionTaxonomy, QuestionTaxonomyStoredSuggestionDto?>
{
    public async ValueTask<QuestionTaxonomyStoredSuggestionDto?> Handle(AdminSuggestQuestionTaxonomy r, CancellationToken ct)
    {
        var question = await db.Questions
            .Include(q => q.Options)
            .Include(q => q.Tags)
            .FirstOrDefaultAsync(q => q.Id == r.QuestionId, ct);
        if (question is null) return null;

        var request = QuestionTaxonomySuggestionMapper.ToRequest(question);
        var response = await sidecar.SuggestAsync(request, ct);
        if (response is null) return null;

        var suggestion = QuestionTaxonomySuggestionMapper.ToEntity(question.Id, question.SourceDataset, question.SourceQuestionId, response);
        db.QuestionTaxonomySuggestions.Add(suggestion);
        await db.SaveChangesAsync(ct);

        return QuestionTaxonomySuggestionMapper.ToDto(suggestion);
    }
}

public sealed class AdminListQuestionTaxonomySuggestionsHandler(IAppDb db)
    : IRequestHandler<AdminListQuestionTaxonomySuggestions, IReadOnlyList<QuestionTaxonomyStoredSuggestionDto>>
{
    public async ValueTask<IReadOnlyList<QuestionTaxonomyStoredSuggestionDto>> Handle(AdminListQuestionTaxonomySuggestions r, CancellationToken ct)
    {
        var rows = await db.QuestionTaxonomySuggestions
            .AsNoTracking()
            .Where(s => s.QuestionId == r.QuestionId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(ct);
        return rows.Select(QuestionTaxonomySuggestionMapper.ToDto).ToList();
    }
}

public sealed class AdminApplyQuestionTaxonomySuggestionHandler(IAppDb db)
    : IRequestHandler<AdminApplyQuestionTaxonomySuggestion, QuestionDto?>
{
    public async ValueTask<QuestionDto?> Handle(AdminApplyQuestionTaxonomySuggestion r, CancellationToken ct)
    {
        var question = await db.Questions
            .Include(q => q.Options)
            .Include(q => q.Tags)
            .FirstOrDefaultAsync(q => q.Id == r.QuestionId, ct);
        if (question is null) return null;

        var suggestion = await db.QuestionTaxonomySuggestions
            .FirstOrDefaultAsync(s => s.Id == r.Request.SuggestionId && s.QuestionId == r.QuestionId, ct);
        if (suggestion is null) return null;

        var response = QuestionTaxonomySuggestionMapper.ToResponse(suggestion);
        var request = new CreateQuestionRequest(
            question.Text,
            response.DisplayCategory,
            question.Difficulty,
            question.Options.Select(o => new QuestionOptionDto(o.OptionId, o.Text)).ToList(),
            question.CorrectOptionId,
            question.Tags.Select(t => t.Tag).Concat(response.TaxonomyTags).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            question.MediaKey,
            question.Status,
            new QuestionTaxonomyInputDto(
                response.CanonicalCategory,
                response.DisplayCategory,
                response.Subject,
                response.Topic,
                response.Subtopic,
                response.GradeBand,
                response.AgeGroup,
                response.Audience,
                question.SourceDataset,
                response.TaxonomyTags),
            question.SourceQuestionId,
            response.QuestionType,
            response.MediaType);

        question.Update(question.Text, response.DisplayCategory, question.Difficulty, question.CorrectOptionId, question.MediaKey);
        AdminQuestionMutationHelpers.ApplyTaxonomy(question, request);
        suggestion.MarkApplied(r.Request.ReviewedBy, r.Request.ReviewNote);
        await db.SaveChangesAsync(ct);

        var options = question.Options.Select(o => new QuestionOptionDto(o.OptionId, o.Text)).ToList();
        var tags = question.Tags.Select(t => t.Tag).ToList();
        return new QuestionDto(
            question.Id,
            question.Text,
            question.Category,
            question.Difficulty,
            question.Status,
            options,
            question.CorrectOptionId,
            tags,
            question.MediaKey,
            null,
            question.CreatedAtUtc,
            question.UpdatedAtUtc,
            QuestionTaxonomy.ToDto(question));
    }
}

internal static class QuestionTaxonomySuggestionMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static QuestionTaxonomySuggestionRequest ToRequest(Question question) =>
        new(
            question.Text,
            question.Category,
            question.Difficulty,
            question.Options.Select(o => o.Text).ToList(),
            question.Tags.Select(t => t.Tag).ToList(),
            question.SourceDataset,
            question.SourceQuestionId,
            new QuestionTaxonomyInputDto(
                question.CanonicalCategory,
                question.DisplayCategory,
                question.Subject,
                question.Topic,
                question.Subtopic,
                question.GradeBand,
                question.AgeGroup,
                question.Audience,
                question.SourceDataset,
                QuestionTaxonomy.ParseTagsJson(question.TaxonomyTagsJson)));

    public static QuestionTaxonomySuggestion ToEntity(
        Guid? questionId,
        string? sourceDataset,
        string? sourceQuestionId,
        QuestionTaxonomySuggestionResponse response) =>
        new(
            questionId,
            sourceDataset,
            sourceQuestionId,
            JsonSerializer.Serialize(response, JsonOptions),
            JsonSerializer.Serialize(response.FieldConfidences, JsonOptions),
            JsonSerializer.Serialize(response.Warnings, JsonOptions),
            response.OverallConfidence,
            response.ModelVersion);

    public static QuestionTaxonomyStoredSuggestionDto ToDto(QuestionTaxonomySuggestion row) =>
        new(
            row.Id,
            row.QuestionId,
            row.SourceDataset,
            row.SourceQuestionId,
            row.Status,
            ToResponse(row),
            row.CreatedAtUtc,
            row.AppliedAtUtc,
            row.ReviewedBy,
            row.ReviewNote);

    public static QuestionTaxonomySuggestionResponse ToResponse(QuestionTaxonomySuggestion row) =>
        JsonSerializer.Deserialize<QuestionTaxonomySuggestionResponse>(row.SuggestedTaxonomyJson, JsonOptions)
        ?? new QuestionTaxonomySuggestionResponse(
            "general",
            "General",
            "general",
            null,
            null,
            null,
            null,
            "general",
            "multiple_choice",
            "text",
            [],
            new Dictionary<string, decimal>(),
            row.OverallConfidence,
            row.ModelVersion,
            []);
}
