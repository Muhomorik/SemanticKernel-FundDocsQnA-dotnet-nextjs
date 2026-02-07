namespace YieldRaccoon.Wpf.Services;

/// <summary>
/// Service for showing the AboutFund browser window from ViewModels.
/// </summary>
public interface IAboutFundWindowService
{
    /// <summary>
    /// Shows the AboutFund window (non-modal).
    /// If the window is already open, brings it to focus.
    /// </summary>
    void ShowAboutFundWindow();

    /// <summary>
    /// Gets whether the AboutFund window is currently open.
    /// </summary>
    bool IsAboutFundWindowOpen { get; }
}
