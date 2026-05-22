using MediatR;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Economy;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Quiz;

public sealed class CompleteQuizHandler(EconomyService economy, IAppDb db)
    : IRequestHandler<CompleteQuiz, CompleteQuizResponse>
{
    public async Task<CompleteQuizResponse> Handle(CompleteQuiz request, CancellationToken ct)
    {
        var lines = new List<EconomyLineDto>();
        if (request.XpEarned > 0)    lines.Add(new EconomyLineDto(CurrencyType.Xp, request.XpEarned));
        if (request.CoinsEarned > 0) lines.Add(new EconomyLineDto(CurrencyType.Coins, request.CoinsEarned));

        // EconomyService.ApplyAsync is idempotent on EventId — safe to retry
        var result = await economy.ApplyAsync(new CreateEconomyTxnRequest(
            EventId: request.EventId,
            PlayerId: request.PlayerId,
            Kind: "solo-quiz-complete",
            Lines: lines
        ), ct);

        // Record for mission progress tracking on first successful application only
        if (result.Status == EconomyTxnStatus.Applied)
        {
            var alreadyProcessed = await db.ProcessedGameplayEvents
                .AnyAsync(x => x.EventId == request.EventId && x.PlayerId == request.PlayerId, ct);
            if (!alreadyProcessed)
            {
                db.ProcessedGameplayEvents.Add(
                    new ProcessedGameplayEvent(request.EventId, request.PlayerId, "solo-quiz-complete"));
                await db.SaveChangesAsync(ct);
            }
        }

        return new CompleteQuizResponse(
            result.Status.ToString(),
            result.BalanceXp,
            result.BalanceCoins,
            result.BalanceDiamonds
        );
    }
}
