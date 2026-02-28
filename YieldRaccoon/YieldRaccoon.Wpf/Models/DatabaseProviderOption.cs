using YieldRaccoon.Wpf.Configuration;

namespace YieldRaccoon.Wpf.Models;

/// <summary>
/// Represents a selectable database provider for the settings ComboBox.
/// </summary>
/// <param name="Provider">The enum value.</param>
/// <param name="DisplayName">User-facing label (e.g., "SQLite", "DualWrite (SQLite + Azure SQL)").</param>
public sealed record DatabaseProviderOption(DatabaseProvider Provider, string DisplayName)
{
    /// <summary>
    /// Returns the display name for ComboBox rendering.
    /// </summary>
    public override string ToString() => DisplayName;
}
