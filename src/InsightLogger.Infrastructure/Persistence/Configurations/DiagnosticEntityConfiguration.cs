using InsightLogger.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightLogger.Infrastructure.Persistence.Configurations;

public sealed class DiagnosticEntityConfiguration : IEntityTypeConfiguration<DiagnosticEntity>
{
    public void Configure(EntityTypeBuilder<DiagnosticEntity> builder)
    {
        builder.ToTable("Diagnostics");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasMaxLength(128);
        builder.Property(x => x.AnalysisId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ToolKind).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(128);
        builder.Property(x => x.Code).HasMaxLength(32);
        builder.Property(x => x.Severity).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Message).HasColumnType("TEXT").IsRequired();
        builder.Property(x => x.NormalizedMessage).HasColumnType("TEXT").IsRequired();
        builder.Property(x => x.FilePath).HasMaxLength(512);
        builder.Property(x => x.RawSnippet).HasColumnType("TEXT").IsRequired();
        builder.Property(x => x.Category).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Subcategory).HasMaxLength(128);
        builder.Property(x => x.Fingerprint).HasMaxLength(256);
        builder.Property(x => x.MetadataJson).HasColumnType("TEXT");

        builder.HasIndex(x => x.AnalysisId);
        builder.HasIndex(x => x.Fingerprint);
        builder.HasIndex(x => new { x.AnalysisId, x.OrderIndex });
    }
}
