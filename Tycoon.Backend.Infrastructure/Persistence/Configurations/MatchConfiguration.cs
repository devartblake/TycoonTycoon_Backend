using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
    {
        public void Configure(EntityTypeBuilder<Match> b)
        {
            b.ToTable("matches");
            b.HasKey(x => x.Id);
            b.Property(x => x.HostPlayerId).IsRequired();
            b.Property(x => x.Mode).HasMaxLength(32).IsRequired();
            b.Property(x => x.StartedAt).IsRequired();
            b.Property(x => x.FinishedAt);
            b.Property(x => x.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            b.HasMany(x => x.Rounds)
                .WithOne()
                .HasForeignKey(x => x.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.HostPlayerId);
            b.HasIndex(x => x.StartedAt);
        }
    }
}
