using MediatR;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Study
{
    public sealed record CreateStudySession(
        Guid PlayerId,
        string StudySetId,
        string? Mode,
        int Count) : IRequest<StudySessionDto?>;

    public sealed class CreateStudySessionHandler
        : IRequestHandler<CreateStudySession, StudySessionDto?>
    {
        private readonly IAppDb _db;

        public CreateStudySessionHandler(IAppDb db) => _db = db;

        public async Task<StudySessionDto?> Handle(CreateStudySession request, CancellationToken ct)
        {
            var detail = await StudySetHelpers.BuildStudySetDetailAsync(_db, request.StudySetId, request.PlayerId, request.Count, ct);
            if (detail is null)
                return null;

            var session = new StudySession(
                request.PlayerId,
                detail.Id,
                request.Mode ?? StudySessionModes.SelfTest,
                detail.Title,
                detail.Kind,
                detail.Category,
                detail.Questions.Select(x => x.Id).ToList(),
                detail.Questions.ToDictionary(x => x.Id, x => x.CorrectOptionId));

            _db.StudySessions.Add(session);
            await _db.SaveChangesAsync(ct);

            return StudySessionMapper.ToDto(session);
        }
    }
}
