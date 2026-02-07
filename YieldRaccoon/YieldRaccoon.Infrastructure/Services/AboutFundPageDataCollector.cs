using System.Reactive.Linq;
using System.Reactive.Subjects;
using NLog;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.ValueObjects;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Accumulates data from multiple fetch steps during a single fund detail page visit
/// and signals when all steps have resolved.
/// </summary>
/// <remarks>
/// <para>
/// Owns the in-flight <see cref="AboutFundPageData"/> for the currently visited fund.
/// When <see cref="BeginCollection"/> is called for a new fund, any previous incomplete
/// collection is abandoned — its pending slots are marked
/// <see cref="AboutFundFetchStatus.Failed"/> and emitted on <see cref="Completed"/>.
/// </para>
/// <para>
/// Thread-safe: all slot mutations are serialized via a lock. Observable emissions
/// happen outside the lock to avoid deadlocks with UI-thread subscribers.
/// </para>
/// </remarks>
public class AboutFundPageDataCollector : IAboutFundPageDataCollector, IDisposable
{
    private readonly ILogger _logger;
    private readonly object _lock = new();
    private AboutFundPageData? _current;
    private bool _disposed;

    private readonly Subject<AboutFundPageData> _completed = new();
    private readonly Subject<AboutFundPageData> _stateChanged = new();

    /// <inheritdoc />
    public IObservable<AboutFundPageData> Completed => _completed.AsObservable();

    /// <inheritdoc />
    public IObservable<AboutFundPageData> StateChanged => _stateChanged.AsObservable();

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutFundPageDataCollector"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public AboutFundPageDataCollector(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void BeginCollection(string isin, string orderbookId)
    {
        AboutFundPageData? abandoned = null;

        lock (_lock)
        {
            // Abandon previous incomplete collection
            if (_current is { IsComplete: false })
            {
                _logger.Warn("Abandoning incomplete collection for {0} ({1}/{2} resolved)",
                    _current.Isin, _current.ResolvedCount, _current.TotalSlots);

                abandoned = AbandonCurrent();
            }

            _current = new AboutFundPageData
            {
                Isin = isin,
                OrderBookId = orderbookId
            };
        }

        // Emit outside lock
        if (abandoned != null)
            _completed.OnNext(abandoned);

        _stateChanged.OnNext(_current);
        _logger.Debug("Begin collection for {0} (OrderBookId={1})", isin, orderbookId);
    }

    /// <inheritdoc />
    public void ReceiveChartTimePeriods(string data)
    {
        UpdateSlot(
            nameof(AboutFundPageData.ChartTimePeriods),
            pd => pd with { ChartTimePeriods = AboutFundFetchSlot.Succeeded(data) });
    }

    /// <inheritdoc />
    public void ReceiveSekPerformance(string data)
    {
        UpdateSlot(
            nameof(AboutFundPageData.SekPerformance),
            pd => pd with { SekPerformance = AboutFundFetchSlot.Succeeded(data) });
    }

    /// <inheritdoc />
    public void FailSlot(string slotName, string reason)
    {
        UpdateSlot(slotName, pd => slotName switch
        {
            nameof(AboutFundPageData.ChartTimePeriods) =>
                pd with { ChartTimePeriods = AboutFundFetchSlot.Failed(reason) },
            nameof(AboutFundPageData.SekPerformance) =>
                pd with { SekPerformance = AboutFundFetchSlot.Failed(reason) },
            _ => throw new ArgumentException($"Unknown slot: {slotName}", nameof(slotName))
        });
    }

    /// <summary>
    /// Updates a slot on the current collection, emits state change,
    /// and emits completion when all slots have resolved.
    /// </summary>
    private void UpdateSlot(string slotName, Func<AboutFundPageData, AboutFundPageData> update)
    {
        AboutFundPageData snapshot;
        bool isComplete;

        lock (_lock)
        {
            if (_current == null)
            {
                _logger.Warn("Received data for slot '{0}' but no collection is active", slotName);
                return;
            }

            _current = update(_current);
            snapshot = _current;
            isComplete = _current.IsComplete;
        }

        // Emit outside lock
        _stateChanged.OnNext(snapshot);

        if (isComplete)
        {
            _logger.Info("Collection complete for {0}: {1} ({2}/{3} succeeded)",
                snapshot.Isin,
                snapshot.IsFullySuccessful ? "all succeeded" : "partial",
                snapshot.ResolvedCount - CountFailed(snapshot),
                snapshot.TotalSlots);
            _completed.OnNext(snapshot);
        }
    }

    /// <summary>
    /// Marks all pending slots on the current collection as failed (abandoned)
    /// and returns the final snapshot. Must be called inside <see cref="_lock"/>.
    /// </summary>
    private AboutFundPageData AbandonCurrent()
    {
        var pd = _current!;

        if (!pd.ChartTimePeriods.IsResolved)
            pd = pd with { ChartTimePeriods = AboutFundFetchSlot.Failed("Collection abandoned — navigated away") };

        if (!pd.SekPerformance.IsResolved)
            pd = pd with { SekPerformance = AboutFundFetchSlot.Failed("Collection abandoned — navigated away") };

        _current = pd;
        return pd;
    }

    private static int CountFailed(AboutFundPageData pd) =>
        (pd.ChartTimePeriods.Status == AboutFundFetchStatus.Failed ? 1 : 0)
        + (pd.SekPerformance.Status == AboutFundFetchStatus.Failed ? 1 : 0);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        _completed.Dispose();
        _stateChanged.Dispose();
        _disposed = true;
    }
}
