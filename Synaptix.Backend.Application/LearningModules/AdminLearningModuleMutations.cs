using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Questions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.LearningModules
{
    // ── Create ───────────────────────────────────────────────────────────────────

    public sealed record AdminCreateLearningModule(CreateLearningModuleRequest Request)
        : IRequest<AdminLearningModuleListItemDto>;

    public sealed class AdminCreateLearningModuleHandler
        : IRequestHandler<AdminCreateLearningModule, AdminLearningModuleListItemDto>
    {
        private readonly IAppDb _db;

        public AdminCreateLearningModuleHandler(IAppDb db) => _db = db;

        public async ValueTask<AdminLearningModuleListItemDto> Handle(
            AdminCreateLearningModule request,
            CancellationToken ct)
        {
            var req = request.Request;
            var module = new LearningModule(
                req.Title, req.Description, req.Category,
                req.Difficulty, req.RewardXp, req.RewardCoins);
            LearningModuleAdminHelpers.ApplyTaxonomy(module, req.Taxonomy, req.Category);

            _db.LearningModules.Add(module);
            await _db.SaveChangesAsync(ct);

            return LearningModuleAdminHelpers.ToDto(module, lessonCount: 0);
        }
    }

    // ── Update ───────────────────────────────────────────────────────────────────

    public sealed record AdminUpdateLearningModule(Guid ModuleId, UpdateLearningModuleRequest Request)
        : IRequest<AdminLearningModuleListItemDto?>;

    public sealed class AdminUpdateLearningModuleHandler
        : IRequestHandler<AdminUpdateLearningModule, AdminLearningModuleListItemDto?>
    {
        private readonly IAppDb _db;

        public AdminUpdateLearningModuleHandler(IAppDb db) => _db = db;

        public async ValueTask<AdminLearningModuleListItemDto?> Handle(
            AdminUpdateLearningModule request,
            CancellationToken ct)
        {
            var module = await _db.LearningModules
                .Include(m => m.Lessons)
                .FirstOrDefaultAsync(m => m.Id == request.ModuleId, ct);

            if (module is null) return null;

            var req = request.Request;
            module.Update(req.Title, req.Description, req.Category,
                req.Difficulty, req.RewardXp, req.RewardCoins);
            LearningModuleAdminHelpers.ApplyTaxonomy(module, req.Taxonomy, req.Category);

            await _db.SaveChangesAsync(ct);
            return LearningModuleAdminHelpers.ToDto(module, module.Lessons.Count);
        }
    }

    // ── Publish / Unpublish ───────────────────────────────────────────────────────

    public sealed record AdminPublishLearningModule(Guid ModuleId) : IRequest<bool>;

    public sealed class AdminPublishLearningModuleHandler
        : IRequestHandler<AdminPublishLearningModule, bool>
    {
        private readonly IAppDb _db;

        public AdminPublishLearningModuleHandler(IAppDb db) => _db = db;

        public async ValueTask<bool> Handle(AdminPublishLearningModule request, CancellationToken ct)
        {
            var module = await _db.LearningModules.FindAsync(new object[] { request.ModuleId }, ct);
            if (module is null) return false;

            module.Publish();
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }

    public sealed record AdminUnpublishLearningModule(Guid ModuleId) : IRequest<bool>;

    public sealed class AdminUnpublishLearningModuleHandler
        : IRequestHandler<AdminUnpublishLearningModule, bool>
    {
        private readonly IAppDb _db;

        public AdminUnpublishLearningModuleHandler(IAppDb db) => _db = db;

        public async ValueTask<bool> Handle(AdminUnpublishLearningModule request, CancellationToken ct)
        {
            var module = await _db.LearningModules.FindAsync(new object[] { request.ModuleId }, ct);
            if (module is null) return false;

            module.Unpublish();
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }

    // ── Add Lesson ───────────────────────────────────────────────────────────────

    public sealed record AdminAddLesson(Guid ModuleId, AddModuleLessonRequest Request)
        : IRequest<AdminAddLessonResult>;

    public sealed record AdminAddLessonResult(bool Success, string? Error, Guid? LessonId);

    public sealed class AdminAddLessonHandler
        : IRequestHandler<AdminAddLesson, AdminAddLessonResult>
    {
        private readonly IAppDb _db;

        public AdminAddLessonHandler(IAppDb db) => _db = db;

        public async ValueTask<AdminAddLessonResult> Handle(AdminAddLesson request, CancellationToken ct)
        {
            var moduleExists = await _db.LearningModules
                .AsNoTracking()
                .AnyAsync(m => m.Id == request.ModuleId, ct);

            if (!moduleExists)
                return new AdminAddLessonResult(false, "Module not found.", null);

            var questionExists = await _db.Questions
                .AsNoTracking()
                .AnyAsync(q => q.Id == request.Request.QuestionId, ct);

            if (!questionExists)
                return new AdminAddLessonResult(false, "Question not found.", null);

            var orderExists = await _db.ModuleLessons
                .AsNoTracking()
                .AnyAsync(
                    l => l.ModuleId == request.ModuleId && l.Order == request.Request.Order,
                    ct);

            if (orderExists)
                return new AdminAddLessonResult(
                    false,
                    $"A lesson at order {request.Request.Order} already exists in this module.",
                    null);

            var lesson = new ModuleLesson(
                request.ModuleId,
                request.Request.QuestionId,
                request.Request.Order,
                request.Request.Explanation);

            _db.ModuleLessons.Add(lesson);

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return new AdminAddLessonResult(false,
                    $"A lesson at order {request.Request.Order} already exists in this module.", null);
            }

            return new AdminAddLessonResult(true, null, lesson.Id);
        }
    }

    // ── Remove Lesson ─────────────────────────────────────────────────────────────

    public sealed record AdminRemoveLesson(Guid ModuleId, Guid LessonId) : IRequest<bool>;

    public sealed class AdminRemoveLessonHandler
        : IRequestHandler<AdminRemoveLesson, bool>
    {
        private readonly IAppDb _db;

        public AdminRemoveLessonHandler(IAppDb db) => _db = db;

        public async ValueTask<bool> Handle(AdminRemoveLesson request, CancellationToken ct)
        {
            var lesson = await _db.ModuleLessons
                .FirstOrDefaultAsync(
                    l => l.Id == request.LessonId && l.ModuleId == request.ModuleId,
                    ct);

            if (lesson is null) return false;

            _db.ModuleLessons.Remove(lesson);
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }

    // ── Admin List ────────────────────────────────────────────────────────────────

    public sealed record AdminListLearningModules(string? Category, bool? IsPublished)
        : IRequest<IReadOnlyList<AdminLearningModuleListItemDto>>;

    public sealed class AdminListLearningModulesHandler
        : IRequestHandler<AdminListLearningModules, IReadOnlyList<AdminLearningModuleListItemDto>>
    {
        private readonly IAppDb _db;

        public AdminListLearningModulesHandler(IAppDb db) => _db = db;

        public async ValueTask<IReadOnlyList<AdminLearningModuleListItemDto>> Handle(
            AdminListLearningModules request,
            CancellationToken ct)
        {
            var query = _db.LearningModules.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Category))
                query = query.Where(m => m.Category == request.Category.Trim());

            if (request.IsPublished.HasValue)
                query = query.Where(m => m.IsPublished == request.IsPublished.Value);

            var modules = await query
                .OrderByDescending(m => m.UpdatedAtUtc)
                .Select(m => new
                {
                    m.Id, m.Title, m.Category, m.Difficulty,
                    m.CanonicalCategory, m.Subject, m.Topic, m.GradeBand, m.AgeGroup, m.Audience,
                    LessonCount = m.Lessons.Count,
                    m.RewardXp, m.RewardCoins,
                    m.IsPublished, m.CreatedAtUtc, m.UpdatedAtUtc
                })
                .ToListAsync(ct);

            return modules
                .Select(m => new AdminLearningModuleListItemDto(
                    m.Id, m.Title, m.Category, m.Difficulty,
                    m.LessonCount, m.RewardXp, m.RewardCoins,
                    m.IsPublished, m.CreatedAtUtc, m.UpdatedAtUtc,
                    m.CanonicalCategory, m.Subject, m.Topic, m.GradeBand, m.AgeGroup, m.Audience))
                .ToList();
        }
    }

    // ── Shared helper ─────────────────────────────────────────────────────────────

    internal static class LearningModuleAdminHelpers
    {
        internal static AdminLearningModuleListItemDto ToDto(LearningModule m, int lessonCount) =>
            new(m.Id, m.Title, m.Category, m.Difficulty,
                lessonCount, m.RewardXp, m.RewardCoins,
                m.IsPublished, m.CreatedAtUtc, m.UpdatedAtUtc,
                m.CanonicalCategory, m.Subject, m.Topic, m.GradeBand, m.AgeGroup, m.Audience);

        internal static void ApplyTaxonomy(LearningModule module, QuestionTaxonomyInputDto? taxonomy, string category)
        {
            var resolved = QuestionTaxonomy.Resolve(category, taxonomy, taxonomy?.SourceDataset, null, null, null, null);
            module.SetTaxonomy(
                resolved.CanonicalCategory,
                resolved.Subject,
                resolved.Topic,
                resolved.GradeBand,
                resolved.AgeGroup,
                resolved.Audience);
        }
    }
}
