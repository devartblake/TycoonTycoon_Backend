using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Study
{
    public sealed record GetStudySessionSummary(Guid SessionId, Guid PlayerId)
        : IRequest<StudySessionDto?>;

    public sealed class GetStudySessionSummaryHandler
        : IRequestHandler<GetStudySessionSummary, StudySessionDto?>
    {
        private readonly IAppDb _db;

        public GetStudySessionSummaryHandler(IAppDb db) => _db = db;

        public async ValueTask<StudySessionDto?> Handle(GetStudySessionSummary request, CancellationToken ct)
        {
            var session = await _db.StudySessions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.SessionId && x.PlayerId == request.PlayerId, ct);

            return session is null ? null : StudySessionMapper.ToDto(session);
        }
    }
}
