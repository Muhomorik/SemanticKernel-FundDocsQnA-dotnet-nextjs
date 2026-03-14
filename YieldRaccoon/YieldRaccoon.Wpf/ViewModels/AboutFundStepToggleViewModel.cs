using DevExpress.Mvvm;
using YieldRaccoon.Application.Models;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel for a single crawler step toggle checkbox.
/// </summary>
public class AboutFundStepToggleViewModel : BindableBase
{
    /// <summary>
    /// Gets the step kind this toggle controls.
    /// </summary>
    public AboutFundCollectionStepKind StepKind { get; }

    /// <summary>
    /// Gets the short label for display (e.g. "1M", "3M", "YTD").
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets or sets whether this step is enabled for crawling.
    /// </summary>
    public bool IsEnabled
    {
        get => GetProperty(() => IsEnabled);
        set => SetProperty(() => IsEnabled, value);
    }

    public AboutFundStepToggleViewModel(AboutFundCollectionStepKind stepKind, bool isEnabled)
    {
        StepKind = stepKind;
        Label = ToShortLabel(stepKind);
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// Parameterless constructor for design-time support.
    /// </summary>
    public AboutFundStepToggleViewModel()
    {
        Label = string.Empty;
    }

    private static string ToShortLabel(AboutFundCollectionStepKind kind) => kind switch
    {
        AboutFundCollectionStepKind.Select1Month => "1M",
        AboutFundCollectionStepKind.Select3Months => "3M",
        AboutFundCollectionStepKind.SelectYearToDate => "YTD",
        AboutFundCollectionStepKind.Select1Year => "1Y",
        AboutFundCollectionStepKind.Select3Years => "3Y",
        AboutFundCollectionStepKind.Select5Years => "5Y",
        AboutFundCollectionStepKind.SelectMax => "Max",
        _ => kind.ToString()
    };
}
