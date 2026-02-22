using DevExpress.Mvvm;
using YieldRaccoon.Application.Models;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel for a single fund in the about-fund browsing schedule.
/// </summary>
public class AboutFundScheduleItemViewModel : BindableBase
{
    /// <summary>
    /// Gets or sets the fund's ISIN identifier.
    /// </summary>
    public string Isin
    {
        get => GetProperty(() => Isin);
        set => SetProperty(() => Isin, value);
    }

    /// <summary>
    /// Gets or sets the fund's OrderBookId used in the external URL.
    /// </summary>
    public string? OrderBookId
    {
        get => GetProperty(() => OrderBookId);
        set => SetProperty(() => OrderBookId, value);
    }

    /// <summary>
    /// Gets or sets the fund's display name.
    /// </summary>
    public string Name
    {
        get => GetProperty(() => Name);
        set => SetProperty(() => Name, value);
    }

    /// <summary>
    /// Gets or sets the number of history records for this fund.
    /// </summary>
    public int HistoryRecordCount
    {
        get => GetProperty(() => HistoryRecordCount);
        set => SetProperty(() => HistoryRecordCount, value);
    }

    /// <summary>
    /// Gets or sets the timestamp when this fund was last visited by the orchestrator, or null if never visited.
    /// </summary>
    public DateTimeOffset? LastVisitedAt
    {
        get => GetProperty(() => LastVisitedAt);
        set => SetProperty(() => LastVisitedAt, value, () => RaisePropertyChanged(nameof(LastVisitedDisplay)));
    }

    /// <summary>
    /// Gets a display string for the last visited timestamp: formatted date or "Never".
    /// </summary>
    public string LastVisitedDisplay =>
        LastVisitedAt?.LocalDateTime.ToString("yyyy-MM-dd HH:mm") ?? "Never";

    /// <summary>
    /// Gets or sets whether this is the currently active fund in the browsing session.
    /// </summary>
    public bool IsCurrentFund
    {
        get => GetProperty(() => IsCurrentFund);
        set => SetProperty(() => IsCurrentFund, value);
    }

    /// <summary>
    /// Gets or sets whether this fund has been visited in the current session.
    /// </summary>
    public bool IsCompleted
    {
        get => GetProperty(() => IsCompleted);
        set => SetProperty(() => IsCompleted, value);
    }

    /// <summary>
    /// Creates a ViewModel from a <see cref="AboutFundScheduleItem"/> model.
    /// </summary>
    public static AboutFundScheduleItemViewModel FromModel(AboutFundScheduleItem item)
    {
        return new AboutFundScheduleItemViewModel
        {
            Isin = item.Isin,
            OrderBookId = item.OrderBookId.Value,
            Name = item.Name,
            HistoryRecordCount = item.HistoryRecordCount,
            LastVisitedAt = item.LastVisitedAt,
            IsCurrentFund = false,
            IsCompleted = false
        };
    }
}