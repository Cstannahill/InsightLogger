using InsightLogger.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightLogger.Infrastructure.Persistence.Configurations;

public sealed class AnalysisEntityConfiguration : IEntityTypeConfiguration<AnalysisEntity>
{
    public void Configure(EntityTypeBuilder<AnalysisEntity> builder)
    {
        builder.ToTable("Analyses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasMaxLength(128);
        builder.Property(x => x.InputType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ToolDetected).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Parser).HasMaxLength(128);
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.Property(x => x.RawContentHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ContextJson).HasColumnType("TEXT");
        builder.Property(x => x.RawContent).HasColumnType("TEXT");
        builder.Property(x => x.Notes).HasColumnType("TEXT");

        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.ToolDetected);
        builder.HasIndex(x => x.RawContentHash);

        builder.HasMany(x => x.Diagnostics)
            .WithOne(x => x.Analysis)
            .HasForeignKey(x => x.AnalysisId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Groups)
            .WithOne(x => x.Analysis)
            .HasForeignKey(x => x.AnalysisId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.PatternOccurrences)
            .WithOne(x => x.Analysis)
            .HasForeignKey(x => x.AnalysisId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
