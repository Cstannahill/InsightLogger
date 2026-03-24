using InsightLogger.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightLogger.Infrastructure.Persistence.Configurations;

public sealed class RuleEntityConfiguration : IEntityTypeConfiguration<RuleEntity>
{
    public void Configure(EntityTypeBuilder<RuleEntity> builder)
    {
        builder.ToTable("Rules");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasMaxLength(128);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnType("TEXT");
        builder.Property(x => x.ToolKindCondition).HasMaxLength(32);
        builder.Property(x => x.CodeCondition).HasMaxLength(64);
        builder.Property(x => x.SeverityCondition).HasMaxLength(32);
        builder.Property(x => x.CategoryCondition).HasMaxLength(64);
        builder.Property(x => x.MessageRegexCondition).HasColumnType("TEXT");
        builder.Property(x => x.FilePathRegexCondition).HasColumnType("TEXT");
        builder.Property(x => x.FingerprintCondition).HasMaxLength(128);
        builder.Property(x => x.ProjectNameCondition).HasMaxLength(200);
        builder.Property(x => x.RepositoryCondition).HasMaxLength(200);
        builder.Property(x => x.TitleAction).HasMaxLength(200);
        builder.Property(x => x.ExplanationAction).HasColumnType("TEXT");
        builder.Property(x => x.SuggestedFixesJson).HasColumnType("TEXT");
        builder.Property(x => x.TagsJson).HasColumnType("TEXT");
        builder.Property(x => x.MatchCount).HasDefaultValue(0);

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => new { x.IsEnabled, x.ToolKindCondition });
        builder.HasIndex(x => x.FingerprintCondition);
        builder.HasIndex(x => x.Priority);
        builder.HasIndex(x => x.LastMatchedAtUtc);
        builder.HasIndex(x => x.ProjectNameCondition);
        builder.HasIndex(x => x.RepositoryCondition);
    }
}
