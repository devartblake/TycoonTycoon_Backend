using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.LearningModules
{
    public sealed record GetLearningModule(Guid ModuleId)
        : IRequest<LearningModuleDetailDto?>;

    public sealed class GetLearningModuleHandler
        : IRequestHandler<GetLearningModule, LearningModuleDetailDto?>
    {
        private readonly IAppDb _db;

        public GetLearningModuleHandler(IAppDb db) => _db = db;

        public async ValueTask<LearningModuleDetailDto?> Handle(
            GetLearningModule request,
            CancellationToken ct)
        {
            return await _db.LearningModules
                .AsNoTracking()
                .Where(m => m.Id == request.ModuleId && m.IsPublished)
                .Select(m => new LearningModuleDetailDto(
                    m.Id,
                    m.Title,
                    m.Description,
                    m.Category,
                    m.Difficulty,
                    m.Lessons.Count,
                    m.RewardXp,
                    m.RewardCoins,
                    m.CanonicalCategory,
                    m.Subject,
                    m.Topic,
                    m.GradeBand,
                    m.AgeGroup,
                    m.Audience
                ))
                .FirstOrDefaultAsync(ct);
        }
    }
}
