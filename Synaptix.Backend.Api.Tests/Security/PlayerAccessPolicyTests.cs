using FluentAssertions;
using Synaptix.Backend.Api.Security;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Api.Tests.Security;

/// <summary>
/// Guards the access policy that replaced the operator feature-lock: features
/// are withheld only from banned/suspended accounts, everyone else keeps them.
/// </summary>
public sealed class PlayerAccessPolicyTests
{
    [Theory]
    [InlineData(ModerationStatus.Banned, true)]
    [InlineData(ModerationStatus.Restricted, true)]
    [InlineData(ModerationStatus.Suspected, false)]
    [InlineData(ModerationStatus.Normal, false)]
    public void IsFeatureBlocked_OnlyBlocksBannedOrRestricted(ModerationStatus status, bool blocked)
    {
        PlayerAccessFilters.IsFeatureBlocked(status).Should().Be(blocked);
    }
}
