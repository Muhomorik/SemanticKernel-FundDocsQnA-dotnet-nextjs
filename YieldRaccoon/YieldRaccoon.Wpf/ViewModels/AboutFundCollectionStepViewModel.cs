using DevExpress.Mvvm;
using YieldRaccoon.Application.Models;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel for a single interaction step in the collection progress display.
/// </summary>
public class AboutFundCollectionStepViewModel : BindableBase
{
    /// <summary>
    /// Gets or sets the human-readable step name.
    /// </summary>
    public string DisplayName
    {
        get => GetProperty(() => DisplayName);
        set => SetProperty(() => DisplayName, value);
    }

    /// <summary>
    /// Gets or sets the execution status of this step.
    /// </summary>
    public AboutFundCollectionStepStatus Status
    {
        get => GetProperty(() => Status);
        set => SetProperty(() => Status, value, () =>
        {
            RaisePropertyChanged(nameof(StatusIcon));
            RaisePropertyChanged(nameof(StatusBrushKey));
        });
    }

    /// <summary>
    /// Gets or sets the cumulative delay from collection start (display text).
    /// </summary>
    public string DelayText
    {
        get => GetProperty(() => DelayText);
        set => SetProperty(() => DelayText, value);
    }

    /// <summary>
    /// Gets or sets whether this is the currently executing step.
    /// </summary>
    public bool IsCurrent
    {
        get => GetProperty(() => IsCurrent);
        set => SetProperty(() => IsCurrent, value, () =>
        {
            RaisePropertyChanged(nameof(StatusIcon));
            RaisePropertyChanged(nameof(StatusBrushKey));
        });
    }

    /// <summary>
    /// Gets the Segoe Fluent Icons glyph for this step's status.
    /// </summary>
    public string StatusIcon => (Status, IsCurrent) switch
    {
        (AboutFundCollectionStepStatus.Completed, _) => "\uE73E",  // Checkmark
        (AboutFundCollectionStepStatus.Failed, _)    => "\uE783",  // Error badge
        (_, true)                                     => "\uE76C",  // Arrow right
        _                                             => "\uEA3A"   // Circle outline
    };

    /// <summary>
    /// Gets the dynamic resource key for the status brush.
    /// </summary>
    public string StatusBrushKey => (Status, IsCurrent) switch
    {
        (AboutFundCollectionStepStatus.Completed, _) => "yr.SuccessBrush",
        (AboutFundCollectionStepStatus.Failed, _)    => "yr.ErrorBrush",
        (_, true)                                     => "MahApps.Brushes.Accent",
        _                                             => "MahApps.Brushes.Gray5"
    };

    /// <summary>
    /// Updates this ViewModel's properties from a domain step and its position.
    /// </summary>
    public void Update(AboutFundCollectionStep step, bool isCurrent)
    {
        DisplayName = ToDisplayName(step.Kind);
        Status = step.Status;
        DelayText = $"{step.Delay.TotalSeconds:F1}s";
        IsCurrent = isCurrent;
    }

    /// <summary>
    /// Maps <see cref="AboutFundCollectionStepKind"/> to a human-readable label.
    /// </summary>
    public static string ToDisplayName(AboutFundCollectionStepKind kind) => kind switch
    {
        AboutFundCollectionStepKind.ActivateSekView  => "Activate SEK view",
        AboutFundCollectionStepKind.Select1Month     => "Select 1 month",
        AboutFundCollectionStepKind.Select3Months    => "Select 3 months",
        AboutFundCollectionStepKind.SelectYearToDate => "Select year to date",
        AboutFundCollectionStepKind.Select1Year      => "Select 1 year",
        AboutFundCollectionStepKind.Select3Years     => "Select 3 years",
        AboutFundCollectionStepKind.Select5Years     => "Select 5 years",
        AboutFundCollectionStepKind.SelectMax        => "Select max",
        _                                            => kind.ToString()
    };
}
