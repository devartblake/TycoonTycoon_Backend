using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Study
{
    public sealed record GetStudySet(string Id, Guid? PlayerId, int Count)
        : IRequest<StudySetDetailDto?>;

    public sealed class GetStudySetHandler
        : IRequestHandler<GetStudySet, StudySetDetailDto?>
    {
        private readonly IAppDb _db;

        public GetStudySetHandler(IAppDb db) => _db = db;

        public async Task<StudySetDetailDto?> Handle(GetStudySet request, CancellationToken ct)
        {
            return await StudySetHelpers.BuildStudySetDetailAsync(_db, request.Id, request.PlayerId, request.Count, ct);
        }
    }
}
