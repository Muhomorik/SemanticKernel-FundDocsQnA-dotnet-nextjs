using System.Collections.ObjectModel;
using DevExpress.Mvvm;
using NLog;
using YieldRaccoon.Application.Models;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel for the left panel showing the fund browsing schedule.
/// </summary>
public class AboutFundScheduleViewModel : ViewModelBase
{
    private readonly ILogger _logger;

    /// <summary>
    /// Gets the collection of scheduled funds.
    /// </summary>
    public ObservableCollection<AboutFundScheduleItemViewModel> Funds { get; } = new();

    /// <summary>
    /// Gets or sets the currently active fund.
    /// </summary>
    public AboutFundScheduleItemViewModel? CurrentFund
    {
        get => GetProperty(() => CurrentFund);
        set => SetProperty(() => CurrentFund, value);
    }

    /// <summary>
    /// Gets or sets the current index in the schedule.
    /// </summary>
    public int CurrentIndex
    {
        get => GetProperty(() => CurrentIndex);
        set => SetProperty(() => CurrentIndex, value);
    }

    /// <summary>
    /// Gets or sets the total number of funds in the schedule.
    /// </summary>
    public int TotalFunds
    {
        get => GetProperty(() => TotalFunds);
        set => SetProperty(() => TotalFunds, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutFundScheduleViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public AboutFundScheduleViewModel(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Design-time constructor for XAML previewer.
    /// </summary>
    public AboutFundScheduleViewModel()
    {
        _logger = LogManager.GetCurrentClassLogger();
    }

    /// <inheritdoc />
    protected override void OnInitializeInDesignMode()
    {
        Funds.Add(new AboutFundScheduleItemViewModel { Name = "Spiltan Aktiefond Investmentbolag", Isin = "SE0000523868", OrderBookId = "325406", HistoryRecordCount = 2, IsCompleted = true });
        Funds.Add(new AboutFundScheduleItemViewModel { Name = "Avanza Zero", Isin = "SE0001718388", OrderBookId = "325408", HistoryRecordCount = 5, IsCompleted = true });
        Funds.Add(new AboutFundScheduleItemViewModel { Name = "Länsförsäkringar Global Index", Isin = "SE0000740698", OrderBookId = "325410", HistoryRecordCount = 8, IsCurrentFund = true });
        Funds.Add(new AboutFundScheduleItemViewModel { Name = "Swedbank Robur Technology", Isin = "SE0000537892", OrderBookId = "325412", HistoryRecordCount = 12 });
        Funds.Add(new AboutFundScheduleItemViewModel { Name = "SEB Sverige Indexfond", Isin = "SE0000434324", OrderBookId = "325414", HistoryRecordCount = 15 });
        Funds.Add(new AboutFundScheduleItemViewModel { Name = "Handelsbanken Hållbar Energi", Isin = "SE0001165180", OrderBookId = "325416", HistoryRecordCount = 20 });
        Funds.Add(new AboutFundScheduleItemViewModel { Name = "Öhman Global Growth", Isin = "SE0009723232", OrderBookId = "325418", HistoryRecordCount = 25 });
        Funds.Add(new AboutFundScheduleItemViewModel { Name = "Carnegie Sverigefond", Isin = "SE0000429209", OrderBookId = "325420", HistoryRecordCount = 30 });
        Funds.Add(new AboutFundScheduleItemViewModel { Name = "Nordnet Indexfond Sverige", Isin = "SE0006027546", OrderBookId = "325422", HistoryRecordCount = 35 });
        Funds.Add(new AboutFundScheduleItemViewModel { Name = "AMF Räntefond Kort", Isin = "SE0000739567", OrderBookId = "325424", HistoryRecordCount = 42 });
        TotalFunds = 10;
        CurrentIndex = 2;
        CurrentFund = Funds[2];
    }

    /// <summary>
    /// Loads the fund schedule from the given items.
    /// </summary>
    /// <param name="items">The fund schedule items to display.</param>
    public void LoadSchedule(IReadOnlyList<AboutFundScheduleItem> items)
    {
        Funds.Clear();

        foreach (var item in items)
        {
            Funds.Add(AboutFundScheduleItemViewModel.FromModel(item));
        }

        TotalFunds = items.Count;
        _logger.Debug("Loaded {0} funds into schedule view", items.Count);
    }

    /// <summary>
    /// Marks the fund at the given index as the current fund.
    /// </summary>
    /// <param name="index">Zero-based index of the current fund.</param>
    public void MarkCurrentFund(int index)
    {
        // Reset previous current
        if (CurrentFund != null)
            CurrentFund.IsCurrentFund = false;

        if (index >= 0 && index < Funds.Count)
        {
            CurrentFund = Funds[index];
            CurrentFund.IsCurrentFund = true;
            CurrentIndex = index;
        }
    }

    /// <summary>
    /// Marks the fund at the given index as completed.
    /// </summary>
    /// <param name="index">Zero-based index of the completed fund.</param>
    public void MarkCompleted(int index)
    {
        if (index >= 0 && index < Funds.Count)
        {
            Funds[index].IsCompleted = true;
        }
    }
}
