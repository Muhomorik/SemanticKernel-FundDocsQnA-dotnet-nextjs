namespace YieldRaccoon.Wpf.Configuration;

/// <summary>
/// Configuration options for database persistence.
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Default database filename used when no custom path is configured.
    /// </summary>
    public const string DefaultDatabaseFileName = "YieldRaccoon.db";

    /// <summary>
    /// Gets or sets the database provider to use.
    /// </summary>
    /// <remarks>
    /// Valid values: "InMemory", "SQLite", "DualWrite".
    /// Default: "InMemory" for development/testing.
    /// </remarks>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.InMemory;

    /// <summary>
    /// Gets or sets the connection string for the database.
    /// </summary>
    /// <remarks>
    /// For SQLite, this is typically: "Data Source=YieldRaccoon.db"
    /// For InMemory provider, this is ignored.
    /// </remarks>
    public string ConnectionString { get; set; } = $"Data Source={DefaultDatabaseFileName}";
}

/// <summary>
/// Available database providers.
/// </summary>
public enum DatabaseProvider
{
    /// <summary>
    /// Use in-memory repositories (no persistence between sessions).
    /// </summary>
    InMemory,

    /// <summary>
    /// Use SQLite database for local persistence.
    /// </summary>
    SQLite,

    /// <summary>
    /// Write to both SQLite (local) and Azure SQL Database (cloud).
    /// Not yet implemented — falls back to SQLite.
    /// </summary>
    DualWrite
}
