using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Personalization;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations;

public sealed class PersonalizationRuleConfiguration : IEntityTypeConfiguration<PersonalizationRule>
{
    public void Configure(EntityTypeBuilder<PersonalizationRule> b)
    {
        b.ToTable("personalization_rules");
        b.HasKey(x => x.Id);

        b.Property(x => x.RuleKey).HasColumnName("rule_key").HasMaxLength(256);
        b.HasIndex(x => x.RuleKey).IsUnique().HasDatabaseName("ix_personalization_rules_rule_key");

        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(512);
        b.Property(x => x.IsEnabled).HasColumnName("is_enabled");
        b.Property(x => x.RuleJson).HasColumnName("rule_json").HasColumnType("jsonb");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
