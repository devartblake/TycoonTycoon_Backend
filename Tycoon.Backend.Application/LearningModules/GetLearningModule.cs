using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.LearningModules
{
    public sealed record GetLearningModule(Guid ModuleId)
        : IRequest<LearningModuleDetailDto?>;

    public sealed class GetLearningModuleHandler
        : IRequestHandler<GetLearningModule, LearningModuleDetailDto?>
    {
        private readonly IAppDb _db;

        public GetLearningModuleHandler(IAppDb db) => _db = db;

        public async Task<LearningModuleDetailDto?> Handle(
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
                    m.RewardCoins
                ))
                .FirstOrDefaultAsync(ct);
        }
    }
}
