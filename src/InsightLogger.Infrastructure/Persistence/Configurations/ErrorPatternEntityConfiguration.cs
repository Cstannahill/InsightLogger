using InsightLogger.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightLogger.Infrastructure.Persistence.Configurations;

public sealed class ErrorPatternEntityConfiguration : IEntityTypeConfiguration<ErrorPatternEntity>
{
    public void Configure(EntityTypeBuilder<ErrorPatternEntity> builder)
    {
        builder.ToTable("ErrorPatterns");

        builder.HasKey(x => x.Fingerprint);

        builder.Property(x => x.Fingerprint).HasMaxLength(256);
        builder.Property(x => x.Title).HasMaxLength(256);
        builder.Property(x => x.CanonicalMessage).HasColumnType("TEXT").IsRequired();
        builder.Property(x => x.ToolKind).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DiagnosticCode).HasMaxLength(64);
        builder.Property(x => x.LastSuggestedFix).HasColumnType("TEXT");

        builder.HasIndex(x => x.LastSeenAtUtc);
        builder.HasIndex(x => x.ToolKind);
        builder.HasIndex(x => x.Category);
    }
}
