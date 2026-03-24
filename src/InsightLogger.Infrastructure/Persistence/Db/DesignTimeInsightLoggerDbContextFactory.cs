using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InsightLogger.Infrastructure.Persistence.Db;

public sealed class DesignTimeInsightLoggerDbContextFactory : IDesignTimeDbContextFactory<InsightLoggerDbContext>
{
    private const string DefaultConnectionString = "Data Source=App_Data/insightlogger.db";
    private const string ConnectionStringArgPrefix = "--connection=";
    private const string ConnectionStringEnvVar = "INSIGHTLOGGER_PERSISTENCE_CONNECTION_STRING";

    public InsightLoggerDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString(args);
        EnsureSqliteDirectoryExists(connectionString);

        var optionsBuilder = new DbContextOptionsBuilder<InsightLoggerDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        return new InsightLoggerDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith(ConnectionStringArgPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var explicitConnection = arg[ConnectionStringArgPrefix.Length..].Trim();
                if (!string.IsNullOrWhiteSpace(explicitConnection))
                {
                    return explicitConnection;
                }
            }
        }

        var fromEnvironment = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment.Trim();
        }

        return DefaultConnectionString;
    }

    private static void EnsureSqliteDirectoryExists(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource) || builder.DataSource == ":memory:")
        {
            return;
        }

        var fullPath = Path.GetFullPath(builder.DataSource);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
    }
}
