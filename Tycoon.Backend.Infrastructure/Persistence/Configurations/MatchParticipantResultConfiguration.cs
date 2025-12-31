using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class MatchParticipantResultConfiguration : IEntityTypeConfiguration<MatchParticipantResult>
    {
        public void Configure(EntityTypeBuilder<MatchParticipantResult> b)
        {
            b.ToTable("match_participant_results");
            b.HasKey(x => x.Id);

            b.Property(x => x.MatchResultId).IsRequired();
            b.HasIndex(x => x.MatchResultId);

            b.Property(x => x.PlayerId).IsRequired();
            b.HasIndex(x => new { x.MatchResultId, x.PlayerId }).IsUnique();

            b.Property(x => x.Score).IsRequired();
            b.Property(x => x.Correct).IsRequired();
            b.Property(x => x.Wrong).IsRequired();
            b.Property(x => x.AvgAnswerTimeMs).IsRequired();
        }
    }
}
