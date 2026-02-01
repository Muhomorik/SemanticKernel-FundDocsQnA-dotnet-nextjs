using DevExpress.Mvvm;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel wrapper for <see cref="ScheduledBatchItem"/> that implements INotifyPropertyChanged
/// for proper WPF data binding and change notification.
/// </summary>
public class ScheduledBatchItemViewModel : BindableBase
{
    /// <summary>
    /// Gets or sets the batch number (1-based).
    /// </summary>
    public BatchNumber BatchNumber
    {
        get => GetValue<BatchNumber>();
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
    public BatchStatus Status
    {
        get => GetValue<BatchStatus>();
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
    /// Updates this ViewModel from a <see cref="ScheduledBatchItem"/> model.
    /// </summary>
    /// <param name="item">The source item to update from.</param>
    public void UpdateFrom(ScheduledBatchItem item)
    {
        BatchNumber = item.BatchNumber;
        ScheduledAt = item.ScheduledAt;
        Status = item.Status;
        FundsLoaded = item.FundsLoaded;
    }

    /// <summary>
    /// Creates a new <see cref="ScheduledBatchItemViewModel"/> from a <see cref="ScheduledBatchItem"/>.
    /// </summary>
    /// <param name="item">The source item.</param>
    /// <returns>A new ViewModel instance.</returns>
    public static ScheduledBatchItemViewModel FromModel(ScheduledBatchItem item)
    {
        var vm = new ScheduledBatchItemViewModel();
        vm.UpdateFrom(item);
        return vm;
    }
}
