using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tycoon.Shared.Contracts.Dtos
{
    internal class SkillTreeDtos
    {
    }
}
namespace Tycoon.Shared.Contracts.Dtos
{
    public enum SkillBranch
    {
        Knowledge = 1,
        Strategy = 2,
        Powerups = 3
    }

    public sealed record SkillCostDto(CurrencyType Currency, int Amount);

    public sealed record SkillNodeDto(
        string Key,
        SkillBranch Branch,
        int Tier,
        string Title,
        string Description,
        IReadOnlyList<string> PrereqKeys,
        IReadOnlyList<SkillCostDto> Costs,
        IReadOnlyDictionary<string, double> Effects
    );

    public sealed record SkillTreeCatalogDto(IReadOnlyList<SkillNodeDto> Nodes);

    public sealed record PlayerSkillStateDto(
        Guid PlayerId,
        IReadOnlyList<string> UnlockedKeys
    );

    public sealed record UnlockSkillRequest(
        Guid EventId,
        Guid PlayerId,
        string NodeKey
    );

    public sealed record UnlockSkillResultDto(
        Guid EventId,
        Guid PlayerId,
        string NodeKey,
        string Status, // "Unlocked" | "Duplicate" | "MissingPrereq" | "NotFound" | "InsufficientFunds"
        IReadOnlyList<string> UnlockedKeys
    );

    public sealed record RespecSkillsRequest(
        Guid EventId,
        Guid PlayerId,
        int RefundPercent // e.g. 80 means 80% refund of spent currency
    );

    public sealed record RespecSkillsResultDto(
        Guid EventId,
        Guid PlayerId,
        string Status, // "Respecced" | "Duplicate"
        int RefundedCoins,
        int RefundedDiamonds,
        IReadOnlyList<string> UnlockedKeys
    );
}
