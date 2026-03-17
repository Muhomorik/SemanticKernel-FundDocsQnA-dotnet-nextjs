using DevExpress.Mvvm;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel wrapper for <see cref="FundListScheduledBatchItem"/> that implements INotifyPropertyChanged
/// for proper WPF data binding and change notification.
/// </summary>
public class FundListScheduledBatchItemViewModel : BindableBase
{
    /// <summary>
    /// Gets or sets the batch number (1-based).
    /// </summary>
    public FundListBatchNumber BatchNumber
    {
        get => GetValue<FundListBatchNumber>();
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets the scheduled time for this batch load.
    /// </summary>
    public DateTimeOffset ScheduledAt
    {
        get => GetValue<DateTimeOffset>();
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets the current status of this batch.
    /// </summary>
    public FundListBatchStatus Status
    {
        get => GetValue<FundListBatchStatus>();
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets the number of funds loaded in this batch, or null if not yet completed.
    /// </summary>
    public int? FundsLoaded
    {
        get => GetValue<int?>();
        set => SetValue(value);
    }

    /// <summary>
    /// Updates this ViewModel from a <see cref="FundListScheduledBatchItem"/> model.
    /// </summary>
    /// <param name="item">The source item to update from.</param>
    public void UpdateFrom(FundListScheduledBatchItem item)
    {
        BatchNumber = item.BatchNumber;
        ScheduledAt = item.ScheduledAt;
        Status = item.Status;
        FundsLoaded = item.FundsLoaded;
    }

    /// <summary>
    /// Creates a new <see cref="FundListScheduledBatchItemViewModel"/> from a <see cref="FundListScheduledBatchItem"/>.
    /// </summary>
    /// <param name="item">The source item.</param>
    /// <returns>A new ViewModel instance.</returns>
    public static FundListScheduledBatchItemViewModel FromModel(FundListScheduledBatchItem item)
    {
        var vm = new FundListScheduledBatchItemViewModel();
        vm.UpdateFrom(item);
        return vm;
    }
}
