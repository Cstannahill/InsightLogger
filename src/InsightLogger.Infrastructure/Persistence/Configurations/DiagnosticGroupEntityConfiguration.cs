using InsightLogger.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightLogger.Infrastructure.Persistence.Configurations;

public sealed class DiagnosticGroupEntityConfiguration : IEntityTypeConfiguration<DiagnosticGroupEntity>
{
    public void Configure(EntityTypeBuilder<DiagnosticGroupEntity> builder)
    {
        builder.ToTable("DiagnosticGroups");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasMaxLength(128);
        builder.Property(x => x.AnalysisId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Fingerprint).HasMaxLength(256).IsRequired();
        builder.Property(x => x.GroupReason).HasMaxLength(256);
        builder.Property(x => x.PrimaryDiagnosticId).HasMaxLength(128);
        builder.Property(x => x.RelatedDiagnosticIdsJson).HasColumnType("TEXT").IsRequired();

        builder.HasIndex(x => x.AnalysisId);
        builder.HasIndex(x => x.Fingerprint);
        builder.HasIndex(x => new { x.AnalysisId, x.OrderIndex });
    }
}
