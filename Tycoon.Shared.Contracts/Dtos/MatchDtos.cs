using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tycoon.Shared.Contracts.Dtos
{
    public record StartMatchRequest(Guid HostPlayerId, string Mode);
    public record StartMatchResponse(Guid MatchId, DateTimeOffset StartedAt);
}
