namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Service for showing the settings dialog from ViewModels.
/// </summary>
public interface ISettingsDialogService
{
    /// <summary>
    /// Shows the settings dialog modally.
    /// </summary>
    /// <returns>True if settings were saved, false if cancelled.</returns>
    bool ShowSettingsDialog();
}
