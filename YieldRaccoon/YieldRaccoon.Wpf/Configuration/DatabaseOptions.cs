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

    /// <summary>
    /// Gets or sets the Backend API base URL for the DualWrite provider.
    /// </summary>
    /// <remarks>
    /// Used only when <see cref="Provider"/> is <see cref="DatabaseProvider.DualWrite"/>.
    /// Example: "https://your-app.azurewebsites.net"
    /// </remarks>
    public string? BackendApiUrl { get; set; }

    /// <summary>
    /// Gets or sets the Backend API key for the DualWrite provider.
    /// </summary>
    /// <remarks>
    /// Used only when <see cref="Provider"/> is <see cref="DatabaseProvider.DualWrite"/>.
    /// Sent as "Authorization: ApiKey {key}" header to the Backend API fund sync endpoints.
    /// </remarks>
    public string? BackendApiKey { get; set; }
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
