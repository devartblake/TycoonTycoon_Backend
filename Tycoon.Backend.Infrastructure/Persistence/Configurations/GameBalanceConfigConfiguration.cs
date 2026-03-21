using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations;

public sealed class GameBalanceConfigConfiguration : IEntityTypeConfiguration<GameBalanceConfig>
{
    public void Configure(EntityTypeBuilder<GameBalanceConfig> b)
    {
        b.ToTable("game_balance_configs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(32);
        b.Property(x => x.ConfigJson).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
    }
}
