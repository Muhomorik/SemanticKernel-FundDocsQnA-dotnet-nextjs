using DevExpress.Mvvm;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel for a single data fetch slot chip in the collection progress display.
/// </summary>
public class AboutFundDataSlotViewModel : BindableBase
{
    /// <summary>
    /// Gets or sets the short label for this slot (e.g., "1M", "YTD").
    /// </summary>
    public string Label
    {
        get => GetProperty(() => Label);
        set => SetProperty(() => Label, value);
    }

    /// <summary>
    /// Gets or sets the fetch status for this slot.
    /// </summary>
    public AboutFundFetchStatus Status
    {
        get => GetProperty(() => Status);
        set => SetProperty(() => Status, value, () => RaisePropertyChanged(nameof(StatusBrushKey)));
    }

    /// <summary>
    /// Gets the dynamic resource key for the chip background brush.
    /// </summary>
    public string StatusBrushKey => Status switch
    {
        AboutFundFetchStatus.Succeeded => "yr.SuccessBrush",
        AboutFundFetchStatus.Failed    => "yr.ErrorBrush",
        _                              => "MahApps.Brushes.Gray8"
    };

    /// <summary>
    /// Updates this ViewModel from a slot identifier and fetch slot data.
    /// </summary>
    public void Update(AboutFundDataSlot slot, AboutFundFetchSlot data)
    {
        Label = ToLabel(slot);
        Status = data.Status;
    }

    /// <summary>
    /// Maps <see cref="AboutFundDataSlot"/> to a compact display label.
    /// </summary>
    public static string ToLabel(AboutFundDataSlot slot) => slot switch
    {
        AboutFundDataSlot.Chart1Month     => "1M",
        AboutFundDataSlot.Chart3Months    => "3M",
        AboutFundDataSlot.ChartYearToDate => "YTD",
        AboutFundDataSlot.Chart1Year      => "1Y",
        AboutFundDataSlot.Chart3Years     => "3Y",
        AboutFundDataSlot.Chart5Years     => "5Y",
        AboutFundDataSlot.ChartMax        => "Max",
        _                                 => slot.ToString()
    };
}
