namespace InsightLogger.Infrastructure.Persistence.Db;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public bool Enabled { get; set; } = true;
    public string ConnectionString { get; set; } = "Data Source=App_Data/insightlogger.db";
    public bool AutoMigrate { get; set; } = false;
}
