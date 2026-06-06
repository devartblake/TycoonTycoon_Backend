using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Questions
{
    public sealed record AdminCreateQuestion(CreateQuestionRequest Req) : IRequest<QuestionDto>;
    public sealed record AdminUpdateQuestion(Guid Id, UpdateQuestionRequest Req) : IRequest<QuestionDto?>;
    public sealed record AdminDeleteQuestion(Guid Id) : IRequest<bool>;
    public sealed record AdminBulkDelete(BulkDeleteQuestionsRequest Req) : IRequest<BulkDeleteResultDto>;
    public sealed record AdminImportQuestions(ImportQuestionsRequest Req) : IRequest<ImportQuestionsResultDto>;
    public sealed record AdminImportTaxonomyQuestions(TaxonomyImportQuestionsRequest Req) : IRequest<ImportQuestionsResultDto>;
    public sealed record AdminSetQuestionStatus(Guid Id, string Status) : IRequest<QuestionDto?>;

    public sealed class AdminCreateQuestionHandler(IAppDb db, ILogger<AdminCreateQuestionHandler> logger) : IRequestHandler<AdminCreateQuestion, QuestionDto>
    {
        public async ValueTask<QuestionDto> Handle(AdminCreateQuestion r, CancellationToken ct)
        {
            Validate(r.Req);

            var q = new Question(r.Req.Text, r.Req.Category, r.Req.Difficulty, r.Req.CorrectOptionId, r.Req.MediaKey);
            AdminQuestionMutationHelpers.ApplyTaxonomy(q, r.Req);
            q.SetStatus(r.Req.Status ?? "Draft");

            var options = r.Req.Options.Select(o => new QuestionOption(q.Id, o.Id, o.Text));
            q.ReplaceOptions(options);
            q.ReplaceTags(r.Req.Tags);

            db.Questions.Add(q);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Admin question created: QuestionId={QuestionId}, Category={Category}", q.Id, q.Category);

            return await dbToDto(db, q.Id, ct);
        }

        private static void Validate(CreateQuestionRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Text)) throw new ArgumentException("Question text is required.");
            if (req.Options is null || req.Options.Count < 2) throw new ArgumentException("At least two options are required.");
            if (req.Options.All(o => o.Id != req.CorrectOptionId)) throw new ArgumentException("CorrectOptionId must match an option id.");
        }

        private static async Task<QuestionDto> dbToDto(IAppDb db, Guid id, CancellationToken ct)
        {
            var dto = await new AdminGetQuestionHandler(db).Handle(new AdminGetQuestion(id), ct);
            return dto!;
        }
    }

    public sealed class AdminUpdateQuestionHandler(IAppDb db, ILogger<AdminUpdateQuestionHandler> logger) : IRequestHandler<AdminUpdateQuestion, QuestionDto?>
    {
        public async ValueTask<QuestionDto?> Handle(AdminUpdateQuestion r, CancellationToken ct)
        {
            var q = await db.Questions
                .Include(x => x.Options)
                .Include(x => x.Tags)
                .FirstOrDefaultAsync(x => x.Id == r.Id, ct);
            if (q is null) return null;

            if (string.IsNullOrWhiteSpace(r.Req.Text)) throw new ArgumentException("Question text is required.");
            if (r.Req.Options is null || r.Req.Options.Count < 2) throw new ArgumentException("At least two options are required.");
            if (r.Req.Options.All(o => o.Id != r.Req.CorrectOptionId)) throw new ArgumentException("CorrectOptionId must match an option id.");

            q.Update(r.Req.Text, r.Req.Category, r.Req.Difficulty, r.Req.CorrectOptionId, r.Req.MediaKey);
            AdminQuestionMutationHelpers.ApplyTaxonomy(q, r.Req);
            if (!string.IsNullOrWhiteSpace(r.Req.Status))
                q.SetStatus(r.Req.Status);

            var requestedOptions = r.Req.Options
                .Select(o => new { Id = o.Id.Trim(), Text = o.Text })
                .ToList();
            var requestedOptionIds = requestedOptions.Select(o => o.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            q.Options.RemoveAll(o => !requestedOptionIds.Contains(o.OptionId));
            foreach (var option in requestedOptions)
            {
                var existing = q.Options.FirstOrDefault(o => string.Equals(o.OptionId, option.Id, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                    q.Options.Add(new QuestionOption(q.Id, option.Id, option.Text));
                else
                    existing.UpdateText(option.Text);
            }

            var requestedTags = (r.Req.Tags ?? Array.Empty<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var requestedTagSet = requestedTags.ToHashSet(StringComparer.OrdinalIgnoreCase);
            q.Tags.RemoveAll(t => !requestedTagSet.Contains(t.Tag));
            foreach (var tag in requestedTags)
            {
                if (!q.Tags.Any(t => string.Equals(t.Tag, tag, StringComparison.OrdinalIgnoreCase)))
                    q.Tags.Add(new QuestionTag(q.Id, tag));
            }

            await db.SaveChangesAsync(ct);

            logger.LogInformation("Admin question updated: QuestionId={QuestionId}, Category={Category}", q.Id, q.Category);

            return await new AdminGetQuestionHandler(db).Handle(new AdminGetQuestion(q.Id), ct);
        }
    }

    public sealed class AdminSetQuestionStatusHandler(IAppDb db, ILogger<AdminSetQuestionStatusHandler> logger)
        : IRequestHandler<AdminSetQuestionStatus, QuestionDto?>
    {
        public async ValueTask<QuestionDto?> Handle(AdminSetQuestionStatus r, CancellationToken ct)
        {
            var q = await db.Questions.FirstOrDefaultAsync(x => x.Id == r.Id, ct);
            if (q is null) return null;

            q.SetStatus(r.Status);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Admin question status changed: QuestionId={QuestionId}, Status={Status}", q.Id, q.Status);
            return await new AdminGetQuestionHandler(db).Handle(new AdminGetQuestion(q.Id), ct);
        }
    }

    public sealed class AdminDeleteQuestionHandler(IAppDb db, ILogger<AdminDeleteQuestionHandler> logger) : IRequestHandler<AdminDeleteQuestion, bool>
    {
        public async ValueTask<bool> Handle(AdminDeleteQuestion r, CancellationToken ct)
        {
            var q = await db.Questions.FirstOrDefaultAsync(x => x.Id == r.Id, ct);
            if (q is null) return false;

            db.Questions.Remove(q);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Admin question deleted: QuestionId={QuestionId}", q.Id);
            return true;
        }
    }

    public sealed class AdminBulkDeleteHandler(IAppDb db, ILogger<AdminBulkDeleteHandler> logger) : IRequestHandler<AdminBulkDelete, BulkDeleteResultDto>
    {
        public async ValueTask<BulkDeleteResultDto> Handle(AdminBulkDelete r, CancellationToken ct)
        {
            var ids = (r.Req.Ids ?? Array.Empty<Guid>()).Distinct().ToArray();
            if (ids.Length == 0) return new BulkDeleteResultDto(0, 0);

            var qs = await db.Questions.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
            db.Questions.RemoveRange(qs);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Admin question bulk delete: Requested={Requested}, Deleted={Deleted}", ids.Length, qs.Count);

            return new BulkDeleteResultDto(ids.Length, qs.Count);
        }
    }

    public sealed class AdminImportQuestionsHandler(IAppDb db, ILogger<AdminImportQuestionsHandler> logger) : IRequestHandler<AdminImportQuestions, ImportQuestionsResultDto>
    {
        public async ValueTask<ImportQuestionsResultDto> Handle(AdminImportQuestions r, CancellationToken ct)
        {
            var received = r.Req.Questions?.Count ?? 0;
            var created = 0;
            var failed = 0;

            foreach (var req in r.Req.Questions ?? Array.Empty<CreateQuestionRequest>())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(req.Text) || req.Options.Count < 2) { failed++; continue; }
                    if (req.Options.All(o => o.Id != req.CorrectOptionId)) { failed++; continue; }

                    var q = new Question(req.Text, req.Category, req.Difficulty, req.CorrectOptionId, req.MediaKey);
                    AdminQuestionMutationHelpers.ApplyTaxonomy(q, req);
                    q.SetStatus(req.Status ?? "Draft");
                    q.ReplaceOptions(req.Options.Select(o => new QuestionOption(q.Id, o.Id, o.Text)));
                    q.ReplaceTags(req.Tags);

                    db.Questions.Add(q);
                    created++;
                }
                catch
                {
                    failed++;
                }
            }

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Admin question import: Received={Received}, Created={Created}, Failed={Failed}", received, created, failed);
            return new ImportQuestionsResultDto(received, created, failed);
        }
    }

    public sealed class AdminImportTaxonomyQuestionsHandler(
        IAppDb db,
        IQuestionTaxonomySidecarClient sidecar,
        IOptions<QuestionTaxonomySidecarOptions> sidecarOptions,
        ILogger<AdminImportTaxonomyQuestionsHandler> logger)
        : IRequestHandler<AdminImportTaxonomyQuestions, ImportQuestionsResultDto>
    {
        public async ValueTask<ImportQuestionsResultDto> Handle(AdminImportTaxonomyQuestions r, CancellationToken ct)
        {
            var items = r.Req.Questions ?? Array.Empty<TaxonomyQuestionImportItemDto>();
            var received = items.Count;
            var created = 0;
            var failed = 0;
            var autoApplyEnabled = r.Req.AutoApplyHighConfidenceSuggestions && sidecarOptions.Value.QuestionTaxonomyAutoApplyEnabled;
            var autoApplyConfidence = Math.Max(
                r.Req.MinimumAutoApplyConfidence,
                sidecarOptions.Value.QuestionTaxonomyAutoApplyMinConfidence);

            foreach (var item in items)
            {
                try
                {
                    var workingItem = item;
                    QuestionTaxonomySuggestionResponse? suggestion = null;
                    var taxonomyWasMissing = !AdminQuestionMutationHelpers.HasCompleteTaxonomy(item);
                    if (r.Req.EnrichWithSidecar && taxonomyWasMissing)
                    {
                        suggestion = await sidecar.SuggestAsync(AdminQuestionMutationHelpers.ToSuggestionRequest(item), ct);
                        if (suggestion is not null &&
                            autoApplyEnabled &&
                            suggestion.OverallConfidence >= autoApplyConfidence &&
                            suggestion.Warnings.Count == 0)
                        {
                            workingItem = AdminQuestionMutationHelpers.WithSuggestion(item, suggestion);
                        }
                    }

                    var mapped = AdminQuestionMutationHelpers.MapImportItem(workingItem);
                    if (mapped is null)
                    {
                        failed++;
                        if (r.Req.Strict) throw new ArgumentException("Invalid taxonomy question import item.");
                        continue;
                    }

                    var existing = await FindExistingAsync(mapped.Value.SourceDataset, mapped.Value.SourceQuestionId, mapped.Value.Request.Text, ct);
                    if (existing is null)
                    {
                        var q = new Question(
                            mapped.Value.Request.Text,
                            mapped.Value.Request.Category,
                            mapped.Value.Request.Difficulty,
                            mapped.Value.Request.CorrectOptionId,
                            mapped.Value.Request.MediaKey);
                        AdminQuestionMutationHelpers.ApplyTaxonomy(q, mapped.Value.Request);
                        q.SetStatus(mapped.Value.Request.Status ?? "Approved");
                        q.ReplaceOptions(mapped.Value.Request.Options.Select(o => new QuestionOption(q.Id, o.Id, o.Text)));
                        q.ReplaceTags(mapped.Value.Request.Tags);
                        db.Questions.Add(q);
                        created++;
                        if (suggestion is not null && ReferenceEquals(workingItem, item))
                            db.QuestionTaxonomySuggestions.Add(QuestionTaxonomySuggestionMapper.ToEntity(q.Id, mapped.Value.SourceDataset, mapped.Value.SourceQuestionId, suggestion));
                    }
                    else
                    {
                        existing.Update(
                            mapped.Value.Request.Text,
                            mapped.Value.Request.Category,
                            mapped.Value.Request.Difficulty,
                            mapped.Value.Request.CorrectOptionId,
                            mapped.Value.Request.MediaKey);
                        AdminQuestionMutationHelpers.ApplyTaxonomy(existing, mapped.Value.Request);
                        existing.SetStatus(mapped.Value.Request.Status ?? existing.Status);
                        if (suggestion is not null && ReferenceEquals(workingItem, item))
                            db.QuestionTaxonomySuggestions.Add(QuestionTaxonomySuggestionMapper.ToEntity(existing.Id, mapped.Value.SourceDataset, mapped.Value.SourceQuestionId, suggestion));
                    }
                }
                catch
                {
                    failed++;
                    if (r.Req.Strict) throw;
                }
            }

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Admin taxonomy question import: Received={Received}, Created={Created}, Failed={Failed}", received, created, failed);
            return new ImportQuestionsResultDto(received, created, failed);
        }

        private async Task<Question?> FindExistingAsync(string? sourceDataset, string? sourceQuestionId, string text, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(sourceDataset) && !string.IsNullOrWhiteSpace(sourceQuestionId))
            {
                var bySource = await db.Questions
                    .Include(x => x.Options)
                    .Include(x => x.Tags)
                    .FirstOrDefaultAsync(x => x.SourceDataset == sourceDataset && x.SourceQuestionId == sourceQuestionId, ct);
                if (bySource is not null) return bySource;
            }

            return await db.Questions
                .Include(x => x.Options)
                .Include(x => x.Tags)
                .FirstOrDefaultAsync(x => x.Text == text, ct);
        }
    }

    internal static class AdminQuestionMutationHelpers
    {
    internal readonly record struct MappedImportQuestion(CreateQuestionRequest Request, string? SourceDataset, string? SourceQuestionId);

    internal static MappedImportQuestion? MapImportItem(TaxonomyQuestionImportItemDto item)
    {
        var text = (item.Text ?? item.Question ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;

        var options = NormalizeOptions(item).ToList();
        if (options.Count < 2) return null;

        var correctOptionId = FirstNonBlank(item.CorrectOptionId, ResolveCorrectOptionId(item, options));
        if (string.IsNullOrWhiteSpace(correctOptionId) || options.All(o => o.Id != correctOptionId)) return null;

        var category = FirstNonBlank(item.Category, item.Taxonomy?.DisplayCategory, item.Taxonomy?.CanonicalCategory, "General")!;
        var taxonomy = QuestionTaxonomy.Resolve(
            category,
            item.Taxonomy,
            item.Taxonomy?.SourceDataset,
            item.Id,
            item.Type,
            FirstNonBlank(item.Taxonomy?.TaxonomyTags?.Contains("audio") == true ? "audio" : null, InferMediaType(item)),
            FirstNonBlank(item.MediaKey, item.ImageUrl, item.VideoUrl, item.AudioUrl),
            item.Tags);

        var request = new CreateQuestionRequest(
            Text: text,
            Category: taxonomy.DisplayCategory,
            Difficulty: ParseDifficulty(item.Difficulty),
            Options: options,
            CorrectOptionId: correctOptionId,
            Tags: (item.Tags ?? Array.Empty<string>()).Concat(taxonomy.TaxonomyTags).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            MediaKey: FirstNonBlank(item.MediaKey, item.ImageUrl, item.VideoUrl, item.AudioUrl),
            Status: "Approved",
            Taxonomy: new QuestionTaxonomyInputDto(
                taxonomy.CanonicalCategory,
                taxonomy.DisplayCategory,
                taxonomy.Subject,
                taxonomy.Topic,
                taxonomy.Subtopic,
                taxonomy.GradeBand,
                taxonomy.AgeGroup,
                taxonomy.Audience,
                taxonomy.SourceDataset,
                taxonomy.TaxonomyTags),
            SourceQuestionId: item.Id,
            QuestionType: taxonomy.QuestionType,
            MediaType: taxonomy.MediaType);

        return new MappedImportQuestion(request, taxonomy.SourceDataset, taxonomy.SourceQuestionId);
    }

    internal static bool HasCompleteTaxonomy(TaxonomyQuestionImportItemDto item)
    {
        var t = item.Taxonomy;
        return t is not null &&
               !string.IsNullOrWhiteSpace(t.CanonicalCategory) &&
               !string.IsNullOrWhiteSpace(t.DisplayCategory) &&
               !string.IsNullOrWhiteSpace(t.Subject) &&
               !string.IsNullOrWhiteSpace(t.Audience);
    }

    internal static QuestionTaxonomySuggestionRequest ToSuggestionRequest(TaxonomyQuestionImportItemDto item)
    {
        var options = NormalizeOptions(item).Select(o => o.Text).ToList();
        return new QuestionTaxonomySuggestionRequest(
            Text: (item.Text ?? item.Question ?? string.Empty).Trim(),
            Category: item.Category,
            Difficulty: ParseDifficulty(item.Difficulty),
            Options: options,
            Tags: item.Tags,
            SourceDataset: item.Taxonomy?.SourceDataset,
            SourceQuestionId: item.Id,
            CurrentTaxonomy: item.Taxonomy);
    }

    internal static TaxonomyQuestionImportItemDto WithSuggestion(
        TaxonomyQuestionImportItemDto item,
        QuestionTaxonomySuggestionResponse suggestion) =>
        item with
        {
            Category = suggestion.DisplayCategory,
            Type = suggestion.QuestionType,
            Taxonomy = new QuestionTaxonomyInputDto(
                suggestion.CanonicalCategory,
                suggestion.DisplayCategory,
                suggestion.Subject,
                suggestion.Topic,
                suggestion.Subtopic,
                suggestion.GradeBand,
                suggestion.AgeGroup,
                suggestion.Audience,
                item.Taxonomy?.SourceDataset,
                suggestion.TaxonomyTags)
        };

    internal static void ApplyTaxonomy(Question q, CreateQuestionRequest req)
    {
        var resolved = QuestionTaxonomy.Resolve(
            req.Category,
            req.Taxonomy,
            req.Taxonomy?.SourceDataset,
            req.SourceQuestionId,
            req.QuestionType,
            req.MediaType,
            req.MediaKey,
            req.Tags);

        q.SetTaxonomy(
            resolved.CanonicalCategory,
            resolved.DisplayCategory,
            resolved.Subject,
            resolved.Topic,
            resolved.Subtopic,
            resolved.GradeBand,
            resolved.AgeGroup,
            resolved.Audience,
            resolved.SourceDataset,
            resolved.SourceQuestionId,
            resolved.QuestionType,
            resolved.MediaType,
            QuestionTaxonomy.ToTagsJson(resolved.TaxonomyTags));
    }

    internal static void ApplyTaxonomy(Question q, UpdateQuestionRequest req)
    {
        var resolved = QuestionTaxonomy.Resolve(
            req.Category,
            req.Taxonomy,
            req.Taxonomy?.SourceDataset,
            req.SourceQuestionId,
            req.QuestionType,
            req.MediaType,
            req.MediaKey,
            req.Tags);

        q.SetTaxonomy(
            resolved.CanonicalCategory,
            resolved.DisplayCategory,
            resolved.Subject,
            resolved.Topic,
            resolved.Subtopic,
            resolved.GradeBand,
            resolved.AgeGroup,
            resolved.Audience,
            resolved.SourceDataset,
            resolved.SourceQuestionId,
            resolved.QuestionType,
            resolved.MediaType,
            QuestionTaxonomy.ToTagsJson(resolved.TaxonomyTags));
    }

    private static IEnumerable<QuestionOptionDto> NormalizeOptions(TaxonomyQuestionImportItemDto item)
    {
        var source = item.Answers is { Count: > 0 } ? item.Answers : item.Options ?? Array.Empty<TaxonomyQuestionOptionImportDto>();
        var index = 0;
        foreach (var option in source)
        {
            var text = option.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;
            var id = FirstNonBlank(option.OptionId, option.Id, ((char)('A' + index)).ToString())!;
            index++;
            yield return new QuestionOptionDto(id, text);
        }
    }

    private static string? ResolveCorrectOptionId(TaxonomyQuestionImportItemDto item, IReadOnlyList<QuestionOptionDto> options)
    {
        var marked = (item.Answers ?? item.Options ?? Array.Empty<TaxonomyQuestionOptionImportDto>())
            .FirstOrDefault(o => o.IsCorrect);
        if (marked is not null)
            return FirstNonBlank(marked.OptionId, marked.Id, options.FirstOrDefault(o => o.Text == marked.Text)?.Id);

        if (!string.IsNullOrWhiteSpace(item.CorrectAnswer))
            return options.FirstOrDefault(o => string.Equals(o.Text, item.CorrectAnswer, StringComparison.OrdinalIgnoreCase))?.Id
                ?? item.CorrectAnswer;

        return options.FirstOrDefault()?.Id;
    }

    private static QuestionDifficulty ParseDifficulty(object? value)
    {
        if (value is null) return QuestionDifficulty.Easy;
        if (value is System.Text.Json.JsonElement json)
        {
            if (json.ValueKind == System.Text.Json.JsonValueKind.Number && json.TryGetInt32(out var numeric))
                return Enum.IsDefined(typeof(QuestionDifficulty), numeric) ? (QuestionDifficulty)numeric : QuestionDifficulty.Easy;
            if (json.ValueKind == System.Text.Json.JsonValueKind.String && Enum.TryParse<QuestionDifficulty>(json.GetString(), true, out var parsed))
                return parsed;
        }
        if (value is int i && Enum.IsDefined(typeof(QuestionDifficulty), i)) return (QuestionDifficulty)i;
        if (Enum.TryParse<QuestionDifficulty>(value.ToString(), true, out var difficulty)) return difficulty;
        return int.TryParse(value.ToString(), out var parsedInt) && Enum.IsDefined(typeof(QuestionDifficulty), parsedInt)
            ? (QuestionDifficulty)parsedInt
            : QuestionDifficulty.Easy;
    }

    private static string InferMediaType(TaxonomyQuestionImportItemDto item)
    {
        if (!string.IsNullOrWhiteSpace(item.AudioUrl)) return "audio";
        if (!string.IsNullOrWhiteSpace(item.VideoUrl)) return "video";
        if (!string.IsNullOrWhiteSpace(item.ImageUrl) || !string.IsNullOrWhiteSpace(item.MediaKey)) return "image";
        return "text";
    }

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
    }
}
