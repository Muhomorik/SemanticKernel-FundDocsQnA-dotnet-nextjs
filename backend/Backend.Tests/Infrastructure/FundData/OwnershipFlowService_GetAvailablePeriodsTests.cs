using Backend.API.Infrastructure.FundData;
using Backend.API.Infrastructure.FundData.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.Infrastructure.FundData;

[TestFixture]
[Category("Unit")]
[Category("FundData")]
public class OwnershipFlowService_GetAvailablePeriodsTests
{
    private OwnershipFlowService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        // Service needs IDbContextFactory but GetAvailablePeriods() doesn't use it.
        // Use a dummy factory that throws — proves no DB access happens.
        var contextFactory = new ThrowingDbContextFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new OwnershipFlowService(contextFactory, cache, NullLogger<OwnershipFlowService>.Instance);
    }

    [Test]
    public void GetAvailablePeriods_ReturnsExactly4WeeklyPeriods()
    {
        var result = _sut.GetAvailablePeriods();

        Assert.That(result.Weekly, Has.Count.EqualTo(4));
    }

    [Test]
    public void GetAvailablePeriods_ReturnsExactly3MonthlyPeriods()
    {
        var result = _sut.GetAvailablePeriods();

        Assert.That(result.Monthly, Has.Count.EqualTo(3));
    }

    [Test]
    public void GetAvailablePeriods_MonthlyLabels_AreCorrect()
    {
        var result = _sut.GetAvailablePeriods();

        Assert.That(result.Monthly[0].Label, Is.EqualTo("1 month"));
        Assert.That(result.Monthly[1].Label, Is.EqualTo("2 months"));
        Assert.That(result.Monthly[2].Label, Is.EqualTo("3 months"));
    }

    [Test]
    public void GetAvailablePeriods_MonthlyTo_IsToday()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var result = _sut.GetAvailablePeriods();

        Assert.That(result.Monthly[0].To, Is.EqualTo(today));
        Assert.That(result.Monthly[1].To, Is.EqualTo(today));
        Assert.That(result.Monthly[2].To, Is.EqualTo(today));
    }

    [Test]
    public void GetAvailablePeriods_WeeklyPeriods_AllStartOnMonday()
    {
        var result = _sut.GetAvailablePeriods();

        foreach (var period in result.Weekly)
        {
            var from = DateOnly.Parse(period.From);
            Assert.That(from.DayOfWeek, Is.EqualTo(DayOfWeek.Monday),
                $"Period '{period.Label}' starts on {from.DayOfWeek}, expected Monday");
        }
    }

    [Test]
    public void GetAvailablePeriods_LastWeeklyPeriod_EndsOnTodayOrLater()
    {
        // The most recent week (last in the list) should end on today or today's week's Sunday
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = _sut.GetAvailablePeriods();
        var lastPeriod = result.Weekly[^1];
        var to = DateOnly.Parse(lastPeriod.To);

        Assert.That(to, Is.GreaterThanOrEqualTo(today).Or.EqualTo(today));
    }

    [Test]
    public void GetAvailablePeriods_WeeklyPeriods_AreChronological()
    {
        var result = _sut.GetAvailablePeriods();

        for (var i = 1; i < result.Weekly.Count; i++)
        {
            var prevFrom = DateOnly.Parse(result.Weekly[i - 1].From);
            var currFrom = DateOnly.Parse(result.Weekly[i].From);
            Assert.That(currFrom, Is.GreaterThan(prevFrom),
                $"Period {i} should be after period {i - 1}");
        }
    }

    [Test]
    public void GetAvailablePeriods_WeeklyFromTo_AreParseable()
    {
        var result = _sut.GetAvailablePeriods();

        foreach (var period in result.Weekly)
        {
            Assert.DoesNotThrow(() => DateOnly.Parse(period.From), $"Cannot parse From: {period.From}");
            Assert.DoesNotThrow(() => DateOnly.Parse(period.To), $"Cannot parse To: {period.To}");
        }
    }

    [Test]
    public void GetAvailablePeriods_OlderFullWeeks_EndOnSunday()
    {
        var result = _sut.GetAvailablePeriods();

        // All except the last (current partial week) should end on Sunday
        for (var i = 0; i < result.Weekly.Count - 1; i++)
        {
            var to = DateOnly.Parse(result.Weekly[i].To);
            Assert.That(to.DayOfWeek, Is.EqualTo(DayOfWeek.Sunday),
                $"Period {i} '{result.Weekly[i].Label}' ends on {to.DayOfWeek}, expected Sunday");
        }
    }

    // ─── Monday edge case (from == to regression) ──────────────────────────────
    //
    // Bug: On Monday, GetMonday(today) == today, so the current partial week is
    // MakeWeekPeriod(monday, monday) — from == to. The controller used to reject
    // this with 400 ("from must be earlier than to"), breaking the ownership flow
    // page. Fix: controller allows from == to, service returns empty flow data.

    [Test]
    public void GetAvailablePeriods_MondayCurrentWeek_HasFromEqualsTo()
    {
        // Verify the current week period has from == to on Monday — this is
        // expected behavior. The controller must allow it (no 400).
        var result = _sut.GetAvailablePeriods();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (today.DayOfWeek != DayOfWeek.Monday) Assert.Ignore("Only relevant on Mondays");

        var currentWeek = result.Weekly[^1];
        Assert.That(currentWeek.From, Is.EqualTo(currentWeek.To),
            "On Monday, current partial week should have from == to");
    }

    // ─── FormatPeriodLabel tests (internal static) ──────────────────────────────

    [Test]
    public void FormatPeriodLabel_SameMonth_ShowsMonthOnce()
    {
        var from = new DateOnly(2025, 2, 10);
        var to = new DateOnly(2025, 2, 16);

        var label = OwnershipFlowService.FormatPeriodLabel(from, to);

        Assert.That(label, Is.EqualTo("Feb 10 – 16"));
    }

    [Test]
    public void FormatPeriodLabel_CrossMonth_ShowsBothMonths()
    {
        var from = new DateOnly(2025, 2, 24);
        var to = new DateOnly(2025, 3, 2);

        var label = OwnershipFlowService.FormatPeriodLabel(from, to);

        Assert.That(label, Is.EqualTo("Feb 24 – Mar 2"));
    }

    [Test]
    public void FormatPeriodLabel_CrossYear_ShowsBothMonths()
    {
        var from = new DateOnly(2024, 12, 30);
        var to = new DateOnly(2025, 1, 5);

        var label = OwnershipFlowService.FormatPeriodLabel(from, to);

        Assert.That(label, Is.EqualTo("Dec 30 – Jan 5"));
    }

    /// <summary>
    /// Dummy factory that throws if CreateDbContextAsync is called,
    /// proving that GetAvailablePeriods() does not access the database.
    /// </summary>
    private sealed class ThrowingDbContextFactory : IDbContextFactory<FundDataDbContext>
    {
        public FundDataDbContext CreateDbContext() =>
            throw new InvalidOperationException("GetAvailablePeriods should not access the database");
    }
}
