using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    public sealed record AdminSetQuestionStatus(Guid Id, string Status) : IRequest<QuestionDto?>;

    public sealed class AdminCreateQuestionHandler(IAppDb db, ILogger<AdminCreateQuestionHandler> logger) : IRequestHandler<AdminCreateQuestion, QuestionDto>
    {
        public async ValueTask<QuestionDto> Handle(AdminCreateQuestion r, CancellationToken ct)
        {
            Validate(r.Req);

            var q = new Question(r.Req.Text, r.Req.Category, r.Req.Difficulty, r.Req.CorrectOptionId, r.Req.MediaKey);
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
}
