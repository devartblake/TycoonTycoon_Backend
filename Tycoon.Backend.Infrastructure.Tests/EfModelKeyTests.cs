using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

public sealed class EfModelKeyTests
{
    [Fact]
    public void All_entities_have_primary_keys_or_are_owned_or_keyless()
    {
        var opts = new DbContextOptionsBuilder<Tycoon.Backend.Infrastructure.Persistence.AppDb>()
            .UseInMemoryDatabase("model_key_test") // model build only
            .Options;

        using var db = new Tycoon.Backend.Infrastructure.Persistence.AppDb(opts);

        var offenders = db.Model.GetEntityTypes()
            .Where(et => !et.IsOwned() && et.FindPrimaryKey() is null)
            .Select(et => et.DisplayName())
            .OrderBy(x => x)
            .ToList();

        Assert.True(offenders.Count == 0, "Entities missing PK: " + string.Join(", ", offenders));
    }
}
