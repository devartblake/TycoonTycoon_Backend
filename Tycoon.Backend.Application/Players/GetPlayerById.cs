using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Players
{
    public record GetPlayerById(Guid Id) : IRequest<PlayerDto?>;

    public sealed class GetPlayerByIdHandler(IAppDb db)
        : IRequestHandler<GetPlayerById, PlayerDto?>
    {
        public async Task<PlayerDto?> Handle(GetPlayerById request, CancellationToken ct)
        {
            var p = await db.Players.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

            return p is null
                ? null
                : new PlayerDto(p.Id, p.Username, p.CountryCode, p.Level, p.Xp);
        }
    }
}
