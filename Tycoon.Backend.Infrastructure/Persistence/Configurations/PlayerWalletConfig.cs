using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PlayerWalletConfig : IEntityTypeConfiguration<PlayerWallet>
    {
        public void Configure(EntityTypeBuilder<PlayerWallet> b)
        {
            b.ToTable("player_wallets");
            b.HasKey(x => x.Id);

            b.Property(x => x.PlayerId).IsRequired();
            b.HasIndex(x => x.PlayerId).IsUnique();

            b.Property(x => x.Xp).IsRequired();
            b.Property(x => x.Coins).IsRequired();
            b.Property(x => x.Diamonds).IsRequired();

            b.Property(x => x.UpdatedAtUtc).IsRequired();
        }
    }
}
