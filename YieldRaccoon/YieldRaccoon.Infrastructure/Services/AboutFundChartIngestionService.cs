using System.Text.Json;
using NLog;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Models;

namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Ingests chart data from about-fund page visits into the persistence layer.
/// </summary>
/// <remarks>
/// <para>
/// Implements the full ingestion pipeline:
/// <list type="number">
///   <item>Extracts raw JSON from each succeeded <see cref="AboutFundFetchSlot"/> on the page data.</item>
///   <item>Deserializes each JSON payload into an internal anti-corruption model.</item>
///   <item>Merges all data points across 7 overlapping time periods, deduplicating by NAV date
///         (first occurrence wins, since shorter periods may have finer granularity).</item>
///   <item>Maps deduplicated points to <see cref="FundHistoryRecord"/> entities.</item>
///   <item>Persists via <see cref="IFundHistoryRepository.AddOrUpdateRangeAsync"/>.</item>
/// </list>
/// </para>
/// <para>
/// Failed or empty slots are silently skipped. Deserialization errors for individual slots
/// are logged and skipped without aborting the entire ingestion.
/// </para>
/// </remarks>
public class AboutFundChartIngestionService : IAboutFundChartIngestionService
{
    private readonly ILogger _logger;
    private readonly IFundHistoryRepository _historyRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutFundChartIngestionService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="historyRepository">The fund history repository for persistence.</param>
    public AboutFundChartIngestionService(
        ILogger logger,
        IFundHistoryRepository historyRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
    }

    /// <inheritdoc />
    public async Task<int> IngestChartDataAsync(
        AboutFundPageData pageData,
        IsinId isinId,
        CancellationToken cancellationToken = default)
    {
        var succeededSlots = GetSucceededSlots(pageData);

        if (succeededSlots.Count == 0)
        {
            _logger.Warn("No succeeded chart slots for {0} — skipping ingestion", pageData.OrderBookId);
            return 0;
        }

        _logger.Debug("Ingesting chart data for {0}: {1}/{2} slots succeeded",
            pageData.OrderBookId, succeededSlots.Count, pageData.TotalSlots);

        var allDataPoints = DeserializeAndMerge(succeededSlots, pageData.OrderBookId);

        if (allDataPoints.Count == 0)
        {
            _logger.Warn("No valid data points after deserialization for {0}", pageData.OrderBookId);
            return 0;
        }

        var uniqueByDate = DeduplicateByNavDate(allDataPoints);

        _logger.Info("Chart ingestion for {0}: {1} raw points → {2} unique dates",
            pageData.OrderBookId, allDataPoints.Count, uniqueByDate.Count);

        var historyRecords = MapToHistoryRecords(uniqueByDate, isinId);

        var insertedCount = await _historyRepository.AddRangeIfNotExistsAsync(historyRecords, cancellationToken);
        await _historyRepository.SaveChangesAsync(cancellationToken);

        _logger.Info("Persisted {0} new chart history records for {1} (ISIN: {2}), {3} skipped (already existed)",
            insertedCount, pageData.OrderBookId, isinId.Isin, historyRecords.Count - insertedCount);

        return insertedCount;
    }

    #region Pipeline steps

    /// <summary>
    /// Extracts raw JSON strings from all chart slots that succeeded.
    /// </summary>
    private static List<(AboutFundDataSlot Slot, string Json)> GetSucceededSlots(AboutFundPageData pageData)
    {
        var slots = new List<(AboutFundDataSlot Slot, string Json)>(7);

        TryAdd(slots, AboutFundDataSlot.Chart1Month, pageData.Chart1Month);
        TryAdd(slots, AboutFundDataSlot.Chart3Months, pageData.Chart3Months);
        TryAdd(slots, AboutFundDataSlot.ChartYearToDate, pageData.ChartYearToDate);
        TryAdd(slots, AboutFundDataSlot.Chart1Year, pageData.Chart1Year);
        TryAdd(slots, AboutFundDataSlot.Chart3Years, pageData.Chart3Years);
        TryAdd(slots, AboutFundDataSlot.Chart5Years, pageData.Chart5Years);
        TryAdd(slots, AboutFundDataSlot.ChartMax, pageData.ChartMax);

        return slots;

        static void TryAdd(List<(AboutFundDataSlot, string)> list, AboutFundDataSlot slot, AboutFundFetchSlot fetchSlot)
        {
            if (fetchSlot.IsSucceeded && !string.IsNullOrWhiteSpace(fetchSlot.Data))
                list.Add((slot, fetchSlot.Data!));
        }
    }

    /// <summary>
    /// Deserializes JSON from each succeeded slot and merges all data points into a flat list.
    /// Individual deserialization failures are logged and skipped.
    /// </summary>
    private List<AboutFundChartDataPoint> DeserializeAndMerge(
        List<(AboutFundDataSlot Slot, string Json)> slots,
        OrderBookId orderBookId)
    {
        var allPoints = new List<AboutFundChartDataPoint>();

        foreach (var (slot, json) in slots)
        {
            try
            {
                var response = JsonSerializer.Deserialize<AboutFundChartResponse>(json);

                if (response?.DataSerie is null or { Count: 0 })
                {
                    _logger.Debug("Slot {0} for {1}: deserialized but dataSerie is null/empty",
                        slot, orderBookId);
                    continue;
                }

                allPoints.AddRange(response.DataSerie);
                _logger.Trace("Slot {0} for {1}: {2} data points", slot, orderBookId, response.DataSerie.Count);
            }
            catch (JsonException ex)
            {
                _logger.Warn("Failed to deserialize {0} for {1}: {2}", slot, orderBookId, ex.Message);
            }
        }

        return allPoints;
    }

    /// <summary>
    /// Deduplicates data points by their derived <see cref="DateOnly"/> (NavDate).
    /// When multiple points map to the same date, the first occurrence is kept.
    /// </summary>
    /// <remarks>
    /// Slots are processed in order from shortest to longest time period
    /// (1M, 3M, YTD, 1Y, 3Y, 5Y, Max). The repository's <c>AddOrUpdate</c>
    /// reconciles with any existing records.
    /// </remarks>
    private static List<AboutFundChartDataPoint> DeduplicateByNavDate(List<AboutFundChartDataPoint> points)
    {
        var seen = new HashSet<DateOnly>();
        var unique = new List<AboutFundChartDataPoint>();

        foreach (var point in points)
        {
            var date = ConvertToDateOnly(point.X);
            if (seen.Add(date))
                unique.Add(point);
        }

        return unique;
    }

    /// <summary>
    /// Maps deduplicated chart data points to <see cref="FundHistoryRecord"/> domain entities.
    /// Only <c>Nav</c> and <c>NavDate</c> are populated — chart data does not carry other metrics.
    /// </summary>
    private static List<FundHistoryRecord> MapToHistoryRecords(
        List<AboutFundChartDataPoint> points,
        IsinId isinId)
    {
        return points.Select(p => new FundHistoryRecord
        {
            IsinId = isinId,
            Nav = p.Y,
            NavDate = ConvertToDateOnly(p.X)
        }).ToList();
    }

    /// <summary>
    /// Stockholm timezone for converting chart timestamps to the correct NAV date.
    /// </summary>
    /// <remarks>
    /// Chart API timestamps are midnight CET/CEST (00:00 Stockholm time), which is
    /// 23:00 UTC (winter) or 22:00 UTC (summer) the previous day. Converting via UTC
    /// would lose a day. <c>TimeZoneInfo</c> handles CET↔CEST transitions automatically.
    /// </remarks>
    private static readonly TimeZoneInfo StockholmTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

    /// <summary>
    /// Converts a Unix timestamp in milliseconds to a <see cref="DateOnly"/> in Stockholm time.
    /// </summary>
    private static DateOnly ConvertToDateOnly(long unixMilliseconds)
    {
        var dto = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds);
        var stockholmTime = TimeZoneInfo.ConvertTime(dto, StockholmTimeZone);
        return DateOnly.FromDateTime(stockholmTime.DateTime);
    }

    #endregion

}
