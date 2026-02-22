using NUnit.Framework;

namespace YieldRaccoon.Infrastructure.Tests.Services;

/// <summary>
/// Exploratory tests to understand how chart UTC timestamps
/// relate to listing publication dates for the same NAV value.
/// </summary>
/// <remarks>
/// <para><b>Problem:</b> <c>FundIngestionService</c> (listing) and <c>AboutFundChartIngestionService</c> (chart)
/// both write <c>FundHistoryRecord</c> with a unique constraint on <c>(IsinId, NavDate)</c>.
/// The listing parses NavDate from text; the chart converts Unix timestamps via UTC.
/// For the same NAV value (457.83), the listing reports 2026-02-18 while the chart
/// timestamp <c>1771369200000</c> resolves to 2026-02-17 23:00 UTC → DateOnly = 2026-02-17.</para>
///
/// <para><b>Root cause:</b> Chart API timestamps are midnight CET/CEST (00:00 Stockholm time).
/// In winter (CET, UTC+1) this is 23:00 UTC the previous day; in summer (CEST, UTC+2) it is
/// 22:00 UTC the previous day. Converting to UTC then extracting DateOnly loses a day in both cases.</para>
///
/// <para><b>Confirmed:</b> CET conversion matches the listing date in all 23 test cases:
/// 21 winter (CET) data points including 4 weekend crossings (Fri→Mon), plus 2 summer (CEST)
/// data points from July 2025. <c>TimeZoneInfo("Central European Standard Time")</c> handles
/// CET↔CEST transitions automatically. No NAV data exists on weekends — the chart API
/// simply skips Saturday/Sunday, so no business-day logic is needed.</para>
///
/// <para><b>Fix:</b> Two changes required:
/// <list type="number">
///   <item>Convert chart timestamps to CET/CEST before extracting DateOnly
///         (<c>AboutFundChartIngestionService.ConvertToDateOnly</c>).</item>
///   <item>Chart ingestion must use insert-only semantics (skip existing records) to avoid
///         overwriting listing's richer data (Capital, Risk, SharpeRatio, etc.) with Nav-only
///         chart records. Listing keeps full upsert semantics as the authoritative source.</item>
/// </list>
/// </para>
/// </remarks>
[TestFixture]
public class NavDateConversionTests
{
    ///<summary>
    /// AuAg Silver Bullet A, SE0013358181 
    /// </summary>
    /// <param name="listingDate">Date shown on the listing (publication date)</param>
    /// <param name="chartTimestampMs">Unix timestamp in ms from chart API</param>
    /// <param name="nav">NAV value (same from both sources)</param>
    [TestCase("2025-07-02", 1751407200000L, 207.4)]
    [TestCase("2025-07-03", 1751493600000L, 210.69)]
    [TestCase("2026-01-20", 1768863600000L, 478.50)]
    [TestCase("2026-01-21", 1768950000000L, 472.66)]
    [TestCase("2026-01-22", 1769036400000L, 502.59)]
    [TestCase("2026-01-23", 1769122800000L, 518.56)]
    [TestCase("2026-01-26", 1769382000000L, 520.84)]
    [TestCase("2026-01-27", 1769468400000L, 510.54)]
    [TestCase("2026-01-28", 1769554800000L, 522.66)]
    [TestCase("2026-01-29", 1769641200000L, 501.9)]
    [TestCase("2026-01-30", 1769727600000L, 438.18)]
    [TestCase("2026-02-02", 1769986800000L, 428.98)]
    [TestCase("2026-02-03", 1770073200000L, 456.31)]
    [TestCase("2026-02-04", 1770159600000L, 458.48)]
    [TestCase("2026-02-05", 1770246000000L, 421.05)]
    [TestCase("2026-02-06", 1770332400000L, 448.1)]
    [TestCase("2026-02-09", 1770591600000L, 470.58)]
    [TestCase("2026-02-10", 1770678000000L, 465.0)]
    [TestCase("2026-02-11", 1770764400000L, 476.52)]
    [TestCase("2026-02-12", 1770850800000L, 436.07)]
    [TestCase("2026-02-13", 1770937200000L, 457.84)]
    [TestCase("2026-02-17", 1771282800000L, 437.98)]
    [TestCase("2026-02-18", 1771369200000L, 457.83)]
    public void CompareListingDateToChartTimestamp(
        string listingDate,
        long chartTimestampMs,
        decimal nav)
    {
        // Listing path: text → DateOnly
        var listingNavDate = DateOnly.Parse(listingDate);

        // Chart path: unix ms → UTC → DateOnly (current production logic)
        var chartUtc = DateTimeOffset.FromUnixTimeMilliseconds(chartTimestampMs);
        var chartNavDateUtc = DateOnly.FromDateTime(chartUtc.UtcDateTime);

        // Chart path: unix ms → CET → DateOnly (alternative)
        var cetZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        var chartCet = TimeZoneInfo.ConvertTime(chartUtc, cetZone);
        var chartNavDateCet = DateOnly.FromDateTime(chartCet.DateTime);

        // Chart path: unix ms → next business day
        var chartNavDateNextBizDay = NextBusinessDay(chartNavDateUtc);

        // Output for analysis
        TestContext.WriteLine($"NAV value:           {nav}");
        TestContext.WriteLine($"Listing date:        {listingNavDate}");
        TestContext.WriteLine($"Chart UTC:           {chartUtc:yyyy-MM-dd HH:mm:ss} → DateOnly = {chartNavDateUtc}");
        TestContext.WriteLine($"Chart CET:           {chartCet:yyyy-MM-dd HH:mm:ss} → DateOnly = {chartNavDateCet}");
        TestContext.WriteLine($"Chart UTC+1 day:     {chartNavDateUtc.AddDays(1)}");
        TestContext.WriteLine($"Chart next biz day:  {chartNavDateNextBizDay}");
        TestContext.WriteLine($"---");
        TestContext.WriteLine($"Listing == Chart UTC?          {listingNavDate == chartNavDateUtc}");
        TestContext.WriteLine($"Listing == Chart CET?          {listingNavDate == chartNavDateCet}");
        TestContext.WriteLine($"Listing == Chart UTC+1?        {listingNavDate == chartNavDateUtc.AddDays(1)}");
        TestContext.WriteLine($"Listing == Chart next biz day? {listingNavDate == chartNavDateNextBizDay}");
    }

    private static DateOnly NextBusinessDay(DateOnly date)
    {
        var next = date.AddDays(1);
        while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            next = next.AddDays(1);
        return next;
    }
}
