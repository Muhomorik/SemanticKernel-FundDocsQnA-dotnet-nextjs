using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Services;
using YieldRaccoon.Infrastructure.Tests.AutoFixture;
using YieldRaccoon.Infrastructure.Tests.TestHelpers;

namespace YieldRaccoon.Infrastructure.Tests.Services;

[TestFixture]
[TestOf(typeof(AboutFundScheduleCalculator))]
public class AboutFundScheduleCalculator_RecalculateRemainingScheduleTests
{
    private IFixture _fixture = null!;
    private AboutFundScheduleCalculator _sut = null!;
    private DateTimeOffset _baseTime;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());

        _sut = _fixture.Create<AboutFundScheduleCalculator>();
        _baseTime = new DateTimeOffset(2025, 6, 1, 14, 0, 0, TimeSpan.Zero);
    }

    #region Helpers

    private (List<AboutFundCollectionSchedule> schedules, OrderBookId[] ids) CreateThreeSchedules()
    {
        var ids = Enumerable.Range(0, 3)
            .Select(_ => _fixture.Create<OrderBookId>()).ToArray();
        var start = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var schedules = ids.Select((id, i) =>
            new CollectionScheduleBuilder()
                .WithOrderBookId(id)
                .WithStartTime(start + TimeSpan.FromMinutes(i * 5))
                .WithAllDefaultSteps()
                .Build()).ToList();
        return (schedules, ids);
    }

    #endregion

    [Test]
    public void RecalculateRemainingSchedule_UnknownOrderBookId_ReturnsCopyUnchanged()
    {
        // Arrange
        var (schedules, _) = CreateThreeSchedules();
        var unknownId = _fixture.Create<OrderBookId>();
        var statuses = new Dictionary<OrderBookId, FundVisitStatus>();

        // Act
        var result = _sut.RecalculateRemainingSchedule(schedules, unknownId, _baseTime, statuses);

        // Assert — all entries unchanged
        Assert.That(result, Has.Count.EqualTo(schedules.Count));
        for (var i = 0; i < schedules.Count; i++)
        {
            Assert.That(result[i].StartTime, Is.EqualTo(schedules[i].StartTime));
            Assert.That(result[i].StopTime, Is.EqualTo(schedules[i].StopTime));
        }

        // Result is a new list instance
        Assert.That(result, Is.Not.SameAs(schedules));
    }

    [Test]
    public void RecalculateRemainingSchedule_OriginalListNotMutated()
    {
        // Arrange
        var (schedules, ids) = CreateThreeSchedules();
        var originalTimes = schedules.Select(s => s.StartTime).ToList();
        var statuses = new Dictionary<OrderBookId, FundVisitStatus>();

        // Act
        _ = _sut.RecalculateRemainingSchedule(schedules, ids[1], _baseTime, statuses);

        // Assert — original list untouched
        for (var i = 0; i < schedules.Count; i++)
            Assert.That(schedules[i].StartTime, Is.EqualTo(originalTimes[i]));
    }

    [Test]
    public void RecalculateRemainingSchedule_FromSecondFund_ShiftsToChainFromBaseTime()
    {
        // Arrange
        var (schedules, ids) = CreateThreeSchedules();
        var statuses = new Dictionary<OrderBookId, FundVisitStatus>();

        // Act
        var result = _sut.RecalculateRemainingSchedule(schedules, ids[1], _baseTime, statuses);

        // Assert — fund[1] starts at baseTime + fund[0].InterPageDelay
        var expectedStart = _baseTime + schedules[0].InterPageDelay;
        Assert.That(result[1].StartTime, Is.EqualTo(expectedStart));

        // fund[2] chains after fund[1]
        var expectedStart2 = result[1].StopTime + schedules[1].InterPageDelay;
        Assert.That(result[2].StartTime, Is.EqualTo(expectedStart2));
    }

    [Test]
    public void RecalculateRemainingSchedule_CompletedFundSkipped_CarriedOverUnchanged()
    {
        // Arrange
        var (schedules, ids) = CreateThreeSchedules();
        var statuses = new Dictionary<OrderBookId, FundVisitStatus>
        {
            [ids[1]] = FundVisitStatus.Completed
        };

        // Act
        var result = _sut.RecalculateRemainingSchedule(schedules, ids[1], _baseTime, statuses);

        // Assert — completed fund[1] keeps its original times
        Assert.That(result[1].StartTime, Is.EqualTo(schedules[1].StartTime));
        Assert.That(result[1].StopTime, Is.EqualTo(schedules[1].StopTime));

        // fund[2] (not completed) is shifted
        Assert.That(result[2].StartTime, Is.Not.EqualTo(schedules[2].StartTime));
    }

    [Test]
    public void RecalculateRemainingSchedule_AllFundsNotVisited_AllShifted()
    {
        // Arrange
        var (schedules, ids) = CreateThreeSchedules();
        var statuses = new Dictionary<OrderBookId, FundVisitStatus>();

        // Act — recalculate from the first fund
        var result = _sut.RecalculateRemainingSchedule(schedules, ids[0], _baseTime, statuses);

        // Assert — all funds shifted (baseTime is far from original start)
        Assert.That(result[0].StartTime, Is.EqualTo(_baseTime));
        for (var i = 1; i < result.Count; i++)
        {
            Assert.That(result[i].StartTime,
                Is.EqualTo(result[i - 1].StopTime + schedules[i - 1].InterPageDelay));
        }
    }

    [Test]
    public void RecalculateRemainingSchedule_TotalDurationPreserved()
    {
        // Arrange
        var (schedules, ids) = CreateThreeSchedules();
        var originalDurations = schedules.Select(s => s.TotalDuration).ToList();
        var statuses = new Dictionary<OrderBookId, FundVisitStatus>();

        // Act
        var result = _sut.RecalculateRemainingSchedule(schedules, ids[0], _baseTime, statuses);

        // Assert — every fund keeps its original TotalDuration
        for (var i = 0; i < result.Count; i++)
            Assert.That(result[i].TotalDuration, Is.EqualTo(originalDurations[i]));
    }

    [Test]
    public void RecalculateRemainingSchedule_EntriesBeforeFromIndex_Unchanged()
    {
        // Arrange
        var (schedules, ids) = CreateThreeSchedules();
        var statuses = new Dictionary<OrderBookId, FundVisitStatus>();

        // Act — recalculate from the third fund
        var result = _sut.RecalculateRemainingSchedule(schedules, ids[2], _baseTime, statuses);

        // Assert — first two entries unchanged
        Assert.That(result[0].StartTime, Is.EqualTo(schedules[0].StartTime));
        Assert.That(result[0].StopTime, Is.EqualTo(schedules[0].StopTime));
        Assert.That(result[1].StartTime, Is.EqualTo(schedules[1].StartTime));
        Assert.That(result[1].StopTime, Is.EqualTo(schedules[1].StopTime));

        // Third entry shifted
        Assert.That(result[2].StartTime, Is.Not.EqualTo(schedules[2].StartTime));
    }
}
