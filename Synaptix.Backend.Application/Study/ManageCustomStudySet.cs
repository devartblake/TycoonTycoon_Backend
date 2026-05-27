using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Study
{
    public sealed record CreateCustomStudySet(Guid PlayerId, string Title, string? Description, IReadOnlyList<Guid> QuestionIds)
        : IRequest<StudySetDetailDto?>;

    public sealed record UpdateCustomStudySet(Guid PlayerId, Guid StudySetId, string Title, string? Description, IReadOnlyList<Guid> QuestionIds)
        : IRequest<StudySetDetailDto?>;

    public sealed class CreateCustomStudySetHandler : IRequestHandler<CreateCustomStudySet, StudySetDetailDto?>
    {
        private readonly IAppDb _db;

        public CreateCustomStudySetHandler(IAppDb db) => _db = db;

        public async ValueTask<StudySetDetailDto?> Handle(CreateCustomStudySet request, CancellationToken ct)
        {
            var approvedQuestionIds = await GetApprovedQuestionIdsAsync(request.QuestionIds, ct);
            if (approvedQuestionIds.Count == 0 || string.IsNullOrWhiteSpace(request.Title))
                return null;

            var studySet = new StudySet(request.PlayerId, request.Title, request.Description, approvedQuestionIds);
            _db.StudySets.Add(studySet);
            await _db.SaveChangesAsync(ct);

            return await StudySetHelpers.BuildStudySetDetailAsync(
                _db,
                StudySetHelpers.CreateCustomId(studySet.Id),
                request.PlayerId,
                approvedQuestionIds.Count,
                ct);
        }

        private async Task<List<Guid>> GetApprovedQuestionIdsAsync(IReadOnlyList<Guid> requestedIds, CancellationToken ct)
        {
            var orderedIds = requestedIds.Where(x => x != Guid.Empty).Distinct().ToList();
            var existingIds = await _db.Questions
                .AsNoTracking()
                .Where(x => x.Status == "Approved" && orderedIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(ct);

            return orderedIds.Where(existingIds.Contains).ToList();
        }
    }

    public sealed class UpdateCustomStudySetHandler : IRequestHandler<UpdateCustomStudySet, StudySetDetailDto?>
    {
        private readonly IAppDb _db;

        public UpdateCustomStudySetHandler(IAppDb db) => _db = db;

        public async ValueTask<StudySetDetailDto?> Handle(UpdateCustomStudySet request, CancellationToken ct)
        {
            var studySet = await _db.StudySets
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == request.StudySetId && x.PlayerId == request.PlayerId, ct);

            if (studySet is null)
                return null;

            var approvedQuestionIds = await GetApprovedQuestionIdsAsync(request.QuestionIds, ct);
            if (approvedQuestionIds.Count == 0 || string.IsNullOrWhiteSpace(request.Title))
                return null;

            _db.StudySetItems.RemoveRange(studySet.Items);
            studySet.UpdateMetadata(request.Title, request.Description);
            _db.StudySetItems.AddRange(approvedQuestionIds.Select((questionId, index) =>
                new StudySetItem(studySet.Id, questionId, index)));
            await _db.SaveChangesAsync(ct);

            return await StudySetHelpers.BuildStudySetDetailAsync(
                _db,
                StudySetHelpers.CreateCustomId(studySet.Id),
                request.PlayerId,
                approvedQuestionIds.Count,
                ct);
        }

        private async Task<List<Guid>> GetApprovedQuestionIdsAsync(IReadOnlyList<Guid> requestedIds, CancellationToken ct)
        {
            var orderedIds = requestedIds.Where(x => x != Guid.Empty).Distinct().ToList();
            var existingIds = await _db.Questions
                .AsNoTracking()
                .Where(x => x.Status == "Approved" && orderedIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(ct);

            return orderedIds.Where(existingIds.Contains).ToList();
        }
    }
}
