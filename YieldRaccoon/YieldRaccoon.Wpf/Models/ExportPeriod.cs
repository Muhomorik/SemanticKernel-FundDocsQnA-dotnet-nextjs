namespace YieldRaccoon.Wpf.Models;

/// <summary>
/// Represents a selectable time period for fund data export.
/// </summary>
/// <param name="DisplayName">User-facing label (e.g., "1 week").</param>
/// <param name="Days">Number of days to include from today.</param>
public sealed record ExportPeriod(string DisplayName, int Days)
{
    /// <summary>
    /// Returns the display name for ComboBox rendering.
    /// </summary>
    public override string ToString() => DisplayName;
}
