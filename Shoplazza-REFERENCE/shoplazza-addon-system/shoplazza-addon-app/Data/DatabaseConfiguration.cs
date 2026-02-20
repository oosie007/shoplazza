using Microsoft.EntityFrameworkCore;

namespace ShoplazzaAddonApp.Data;

/// <summary>
/// Database configuration helper for switching between SQL Server and SQLite
/// </summary>
public static class DatabaseConfiguration
{
    /// <summary>
    /// Database provider types
    /// </summary>
    public enum DatabaseProvider
    {
        SqlServer,
        Sqlite
    }

    /// <summary>
    /// Configure database context based on connection string and provider
    /// </summary>
    public static void ConfigureDatabase(
        DbContextOptionsBuilder options,
        string connectionString,
        DatabaseProvider provider)
    {
        switch (provider)
        {
            case DatabaseProvider.SqlServer:
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
                break;

            case DatabaseProvider.Sqlite:
                options.UseSqlite(connectionString, sqliteOptions =>
                {
                    sqliteOptions.CommandTimeout(30);
                });
                break;

            default:
                throw new ArgumentException($"Unsupported database provider: {provider}");
        }

        // Common configurations for all providers
        options.EnableSensitiveDataLogging(false);
        options.EnableServiceProviderCaching(true);
        options.EnableDetailedErrors(true);
    }

    /// <summary>
    /// Determine database provider from connection string
    /// </summary>
    public static DatabaseProvider DetectProvider(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
        }

        // Convert to lowercase for case-insensitive comparison
        var lowerConnectionString = connectionString.ToLowerInvariant();

        // Check for SQLite indicators
        if (lowerConnectionString.Contains("data source=") && 
            (lowerConnectionString.Contains(".db") || lowerConnectionString.Contains(".sqlite")))
        {
            return DatabaseProvider.Sqlite;
        }

        // Check for SQL Server indicators
        if (lowerConnectionString.Contains("server=") || 
            lowerConnectionString.Contains("data source=") && 
            (lowerConnectionString.Contains("localdb") || lowerConnectionString.Contains("mssqllocaldb")))
        {
            return DatabaseProvider.SqlServer;
        }

        // Default to SQL Server for backwards compatibility
        return DatabaseProvider.SqlServer;
    }

    /// <summary>
    /// Get default database value SQL for timestamps based on provider
    /// </summary>
    public static string GetDefaultDateTimeSql(DatabaseProvider provider)
    {
        return provider switch
        {
            DatabaseProvider.SqlServer => "GETUTCDATE()",
            DatabaseProvider.Sqlite => "datetime('now')",
            _ => throw new ArgumentException($"Unsupported database provider: {provider}")
        };
    }

    /// <summary>
    /// Get column type for string fields based on provider and max length
    /// </summary>
    public static string GetStringColumnType(DatabaseProvider provider, int maxLength)
    {
        return provider switch
        {
            DatabaseProvider.SqlServer => $"nvarchar({maxLength})",
            DatabaseProvider.Sqlite => "TEXT",
            _ => throw new ArgumentException($"Unsupported database provider: {provider}")
        };
    }

    /// <summary>
    /// Get recommended connection strings for different environments
    /// </summary>
    public static class DefaultConnectionStrings
    {
        /// <summary>
        /// Default SQL Server LocalDB connection string for development
        /// </summary>
        public static string SqlServerLocalDb =>
            "Server=(localdb)\\mssqllocaldb;Database=ShoplazzaAddonApp;Trusted_Connection=true;MultipleActiveResultSets=true";

        /// <summary>
        /// Default SQLite connection string for development/POC
        /// </summary>
        public static string Sqlite =>
            "Data Source=shoplazza_addon.db";

        /// <summary>
        /// SQLite in-memory connection string for testing
        /// </summary>
        public static string SqliteInMemory =>
            "Data Source=:memory:";

        /// <summary>
        /// Get appropriate default connection string based on environment
        /// </summary>
        public static string GetDefault(string environment = "Development")
        {
            return environment.ToLowerInvariant() switch
            {
                "development" => Sqlite, // Use SQLite for easy development
                "testing" => SqliteInMemory,
                "staging" => SqlServerLocalDb,
                "production" => SqlServerLocalDb, // Should be overridden with actual production connection
                _ => Sqlite
            };
        }
    }

    /// <summary>
    /// Validate connection string format
    /// </summary>
    public static bool IsValidConnectionString(string connectionString, DatabaseProvider provider)
    {
        if (string.IsNullOrEmpty(connectionString))
            return false;

        try
        {
            return provider switch
            {
                DatabaseProvider.SqlServer => connectionString.Contains("Server=") || 
                                            connectionString.Contains("Data Source="),
                DatabaseProvider.Sqlite => connectionString.Contains("Data Source="),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get database file path for SQLite databases
    /// </summary>
    public static string? GetSqliteFilePath(string connectionString)
    {
        if (DetectProvider(connectionString) != DatabaseProvider.Sqlite)
            return null;

        var lowerCs = connectionString.ToLowerInvariant();
        var dataSourceIndex = lowerCs.IndexOf("data source=");
        if (dataSourceIndex == -1)
            return null;

        var valueStart = dataSourceIndex + "data source=".Length;
        var valueEnd = connectionString.IndexOf(';', valueStart);
        if (valueEnd == -1)
            valueEnd = connectionString.Length;

        return connectionString.Substring(valueStart, valueEnd - valueStart).Trim();
    }

    /// <summary>
    /// Ensure SQLite database directory exists
    /// </summary>
    public static void EnsureSqliteDirectoryExists(string connectionString)
    {
        var filePath = GetSqliteFilePath(connectionString);
        if (filePath != null && filePath != ":memory:")
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}