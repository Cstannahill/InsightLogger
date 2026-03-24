using InsightLogger.Infrastructure.Persistence.Configurations;
using InsightLogger.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace InsightLogger.Infrastructure.Persistence.Db;

public sealed class InsightLoggerDbContext : DbContext
{
    public InsightLoggerDbContext(DbContextOptions<InsightLoggerDbContext> options)
        : base(options)
    {
    }

    public DbSet<AnalysisEntity> Analyses => Set<AnalysisEntity>();
    public DbSet<DiagnosticEntity> Diagnostics => Set<DiagnosticEntity>();
    public DbSet<DiagnosticGroupEntity> DiagnosticGroups => Set<DiagnosticGroupEntity>();
    public DbSet<ErrorPatternEntity> ErrorPatterns => Set<ErrorPatternEntity>();
    public DbSet<PatternOccurrenceEntity> PatternOccurrences => Set<PatternOccurrenceEntity>();
    public DbSet<RuleEntity> Rules => Set<RuleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AnalysisEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DiagnosticEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DiagnosticGroupEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ErrorPatternEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PatternOccurrenceEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RuleEntityConfiguration());
    }
}
