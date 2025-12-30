using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Questions
{
    public sealed record AdminCreateQuestion(CreateQuestionRequest Req) : IRequest<QuestionDto>;
    public sealed record AdminUpdateQuestion(Guid Id, UpdateQuestionRequest Req) : IRequest<QuestionDto?>;
    public sealed record AdminDeleteQuestion(Guid Id) : IRequest<bool>;
    public sealed record AdminBulkDelete(BulkDeleteQuestionsRequest Req) : IRequest<BulkDeleteResultDto>;
    public sealed record AdminImportQuestions(ImportQuestionsRequest Req) : IRequest<ImportQuestionsResultDto>;

    public sealed class AdminCreateQuestionHandler(IAppDb db) : IRequestHandler<AdminCreateQuestion, QuestionDto>
    {
        public async Task<QuestionDto> Handle(AdminCreateQuestion r, CancellationToken ct)
        {
            Validate(r.Req);

            var q = new Question(r.Req.Text, r.Req.Category, r.Req.Difficulty, r.Req.CorrectOptionId, r.Req.MediaKey);

            var options = r.Req.Options.Select(o => new QuestionOption(q.Id, o.Id, o.Text));
            q.ReplaceOptions(options);
            q.ReplaceTags(r.Req.Tags);

            db.Questions.Add(q);
            await db.SaveChangesAsync(ct);

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

    public sealed class AdminUpdateQuestionHandler(IAppDb db) : IRequestHandler<AdminUpdateQuestion, QuestionDto?>
    {
        public async Task<QuestionDto?> Handle(AdminUpdateQuestion r, CancellationToken ct)
        {
            var q = await db.Questions.FirstOrDefaultAsync(x => x.Id == r.Id, ct);
            if (q is null) return null;

            if (string.IsNullOrWhiteSpace(r.Req.Text)) throw new ArgumentException("Question text is required.");
            if (r.Req.Options is null || r.Req.Options.Count < 2) throw new ArgumentException("At least two options are required.");
            if (r.Req.Options.All(o => o.Id != r.Req.CorrectOptionId)) throw new ArgumentException("CorrectOptionId must match an option id.");

            q.Update(r.Req.Text, r.Req.Category, r.Req.Difficulty, r.Req.CorrectOptionId, r.Req.MediaKey);

            // Replace options
            var existingOptions = db.QuestionOptions.Where(o => o.QuestionId == q.Id);
            db.QuestionOptions.RemoveRange(existingOptions);
            q.ReplaceOptions(r.Req.Options.Select(o => new QuestionOption(q.Id, o.Id, o.Text)));

            // Replace tags
            var existingTags = db.QuestionTags.Where(t => t.QuestionId == q.Id);
            db.QuestionTags.RemoveRange(existingTags);
            q.ReplaceTags(r.Req.Tags);

            await db.SaveChangesAsync(ct);

            return await new AdminGetQuestionHandler(db).Handle(new AdminGetQuestion(q.Id), ct);
        }
    }

    public sealed class AdminDeleteQuestionHandler(IAppDb db) : IRequestHandler<AdminDeleteQuestion, bool>
    {
        public async Task<bool> Handle(AdminDeleteQuestion r, CancellationToken ct)
        {
            var q = await db.Questions.FirstOrDefaultAsync(x => x.Id == r.Id, ct);
            if (q is null) return false;

            db.Questions.Remove(q);
            await db.SaveChangesAsync(ct);
            return true;
        }
    }

    public sealed class AdminBulkDeleteHandler(IAppDb db) : IRequestHandler<AdminBulkDelete, BulkDeleteResultDto>
    {
        public async Task<BulkDeleteResultDto> Handle(AdminBulkDelete r, CancellationToken ct)
        {
            var ids = (r.Req.Ids ?? Array.Empty<Guid>()).Distinct().ToArray();
            if (ids.Length == 0) return new BulkDeleteResultDto(0, 0);

            var qs = await db.Questions.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
            db.Questions.RemoveRange(qs);
            await db.SaveChangesAsync(ct);

            return new BulkDeleteResultDto(ids.Length, qs.Count);
        }
    }

    public sealed class AdminImportQuestionsHandler(IAppDb db) : IRequestHandler<AdminImportQuestions, ImportQuestionsResultDto>
    {
        public async Task<ImportQuestionsResultDto> Handle(AdminImportQuestions r, CancellationToken ct)
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
            return new ImportQuestionsResultDto(received, created, failed);
        }
    }
}
