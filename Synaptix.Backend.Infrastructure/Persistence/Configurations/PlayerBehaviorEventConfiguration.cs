using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Personalization;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations;

public sealed class PlayerBehaviorEventConfiguration : IEntityTypeConfiguration<PlayerBehaviorEvent>
{
    public void Configure(EntityTypeBuilder<PlayerBehaviorEvent> b)
    {
        b.ToTable("player_behavior_events");
        b.HasKey(x => x.Id);

        b.Property(x => x.PlayerId).HasColumnName("player_id");
        b.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(128);
        b.Property(x => x.EventSource).HasColumnName("event_source").HasMaxLength(128);
        b.Property(x => x.Category).HasColumnName("category").HasMaxLength(128);
        b.Property(x => x.Difficulty).HasColumnName("difficulty").HasMaxLength(64);
        b.Property(x => x.Mode).HasColumnName("mode").HasMaxLength(64);
        b.Property(x => x.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
        b.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        b.Property(x => x.IngestedAt).HasColumnName("ingested_at");

        b.HasIndex(x => new { x.PlayerId, x.OccurredAt })
            .HasDatabaseName("ix_player_behavior_events_player_time");
        b.HasIndex(x => x.EventType).HasDatabaseName("ix_player_behavior_events_type");
        b.HasIndex(x => x.EventSource).HasDatabaseName("ix_player_behavior_events_source");
        b.HasIndex(x => x.Category).HasDatabaseName("ix_player_behavior_events_category");
    }
}
