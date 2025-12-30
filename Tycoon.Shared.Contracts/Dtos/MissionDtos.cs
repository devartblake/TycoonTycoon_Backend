using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tycoon.Shared.Contracts.Dtos
{
    public record MissionDto(Guid Id, string Type, string Key, int Goal, int RewardXp);
}
