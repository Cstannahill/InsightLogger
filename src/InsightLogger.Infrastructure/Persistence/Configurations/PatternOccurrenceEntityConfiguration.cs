using InsightLogger.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightLogger.Infrastructure.Persistence.Configurations;

public sealed class PatternOccurrenceEntityConfiguration : IEntityTypeConfiguration<PatternOccurrenceEntity>
{
    public void Configure(EntityTypeBuilder<PatternOccurrenceEntity> builder)
    {
        builder.ToTable("PatternOccurrences");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasMaxLength(128);
        builder.Property(x => x.Fingerprint).HasMaxLength(256).IsRequired();
        builder.Property(x => x.AnalysisId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DiagnosticId).HasMaxLength(128).IsRequired();

        builder.HasIndex(x => x.Fingerprint);
        builder.HasIndex(x => x.AnalysisId);
        builder.HasIndex(x => x.SeenAtUtc);

        builder.HasOne(x => x.Pattern)
            .WithMany(x => x.Occurrences)
            .HasForeignKey(x => x.Fingerprint)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
