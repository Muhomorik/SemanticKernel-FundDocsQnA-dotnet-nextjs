using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NUnit.Framework;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Services;
using YieldRaccoon.Infrastructure.Tests.AutoFixture;
using YieldRaccoon.Infrastructure.Tests.TestHelpers;

namespace YieldRaccoon.Infrastructure.Tests.Services;

[TestFixture]
[TestOf(typeof(AboutFundScheduleCalculator))]
public class AboutFundScheduleCalculator_CalculateSessionScheduleTests
{
    private IFixture _fixture = null!;
    private Mock<IRandomDelayProvider> _delayProviderMock = null!;
    private AboutFundScheduleCalculator _sut = null!;
    private DateTimeOffset _startTime;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());

        _delayProviderMock = _fixture.Freeze<Mock<IRandomDelayProvider>>();

        // Deterministic: NextDelay(min) = min + 5s, NextDelay() = 10s
        _delayProviderMock
            .Setup(d => d.NextDelay(It.IsAny<TimeSpan>()))
            .Returns<TimeSpan>(min => min + TimeSpan.FromSeconds(5));
        _delayProviderMock
            .Setup(d => d.NextDelay())
            .Returns(TimeSpan.FromSeconds(10));

        _sut = _fixture.Create<AboutFundScheduleCalculator>();
        _startTime = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
    }

    #region Helpers

    private List<AboutFundScheduleItem> CreateFunds(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => new AboutFundScheduleItem
            {
                Isin = _fixture.Create<string>(),
                OrderBookId = _fixture.Create<OrderBookId>(),
                Name = _fixture.Create<string>(),
                HistoryRecordCount = _fixture.Create<int>()
            })
            .ToList();
    }

    private static TimeSpan GetMinDelay(AboutFundCollectionStepKind kind)
        => CollectionScheduleBuilder.DefaultDelays[kind];

    #endregion

    [Test]
    public void CalculateSessionSchedule_EmptyFundList_ReturnsEmptyList()
    {
        // Arrange
        var funds = new List<AboutFundScheduleItem>();

        // Act
        var result = _sut.CalculateSessionSchedule(funds, _startTime, GetMinDelay);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void CalculateSessionSchedule_SingleFund_ReturnsOneScheduleWithCorrectOrderBookId()
    {
        // Arrange
        var funds = CreateFunds(1);

        // Act
        var result = _sut.CalculateSessionSchedule(funds, _startTime, GetMinDelay);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].OrderBookId, Is.EqualTo(funds[0].OrderBookId));
    }

    [Test]
    public void CalculateSessionSchedule_SingleFund_StepCountMatchesAllStepKinds()
    {
        // Arrange
        var funds = CreateFunds(1);

        // Act
        var result = _sut.CalculateSessionSchedule(funds, _startTime, GetMinDelay);

        // Assert
        Assert.That(result[0].Steps, Has.Count.EqualTo(AboutFundCollectionStepKinds.All.Count));
    }

    [Test]
    public void CalculateSessionSchedule_SingleFund_StepsAreChronologicallyOrdered()
    {
        // Arrange
        var funds = CreateFunds(1);

        // Act
        var result = _sut.CalculateSessionSchedule(funds, _startTime, GetMinDelay);

        // Assert
        var steps = result[0].Steps;
        for (var i = 1; i < steps.Count; i++)
            Assert.That(steps[i].FireAt, Is.GreaterThan(steps[i - 1].FireAt));
    }

    [Test]
    public void CalculateSessionSchedule_SingleFund_FirstStepFiresAfterStartTime()
    {
        // Arrange
        var funds = CreateFunds(1);

        // Act
        var result = _sut.CalculateSessionSchedule(funds, _startTime, GetMinDelay);

        // Assert
        Assert.That(result[0].Steps[0].FireAt, Is.GreaterThan(_startTime));
    }

    [Test]
    public void CalculateSessionSchedule_SingleFund_StartTimeMatchesProvidedParameter()
    {
        // Arrange
        var funds = CreateFunds(1);

        // Act
        var result = _sut.CalculateSessionSchedule(funds, _startTime, GetMinDelay);

        // Assert
        Assert.That(result[0].StartTime, Is.EqualTo(_startTime));
    }

    [Test]
    public void CalculateSessionSchedule_TwoFunds_SecondStartsAfterFirstStopPlusInterPageDelay()
    {
        // Arrange
        var funds = CreateFunds(2);

        // Act
        var result = _sut.CalculateSessionSchedule(funds, _startTime, GetMinDelay);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[1].StartTime,
            Is.EqualTo(result[0].StopTime + result[0].InterPageDelay));
    }

    [Test]
    public void CalculateSessionSchedule_ThreeFunds_AllStepKindsQueriedViaDelegate()
    {
        // Arrange
        var funds = CreateFunds(3);
        var queriedKinds = new List<AboutFundCollectionStepKind>();
        TimeSpan TrackingGetMinDelay(AboutFundCollectionStepKind kind)
        {
            queriedKinds.Add(kind);
            return GetMinDelay(kind);
        }

        // Act
        _sut.CalculateSessionSchedule(funds, _startTime, TrackingGetMinDelay);

        // Assert — each step kind queried once per fund
        var expectedCount = AboutFundCollectionStepKinds.All.Count * funds.Count;
        Assert.That(queriedKinds, Has.Count.EqualTo(expectedCount));

        foreach (var kind in AboutFundCollectionStepKinds.All)
        {
            var countForKind = queriedKinds.Count(k => k == kind);
            Assert.That(countForKind, Is.EqualTo(funds.Count),
                $"Expected {kind} to be queried {funds.Count} times");
        }
    }

    [Test]
    public void CalculateSessionSchedule_SingleFund_TotalDurationEqualsStopMinusStart()
    {
        // Arrange
        var funds = CreateFunds(1);

        // Act
        var result = _sut.CalculateSessionSchedule(funds, _startTime, GetMinDelay);

        // Assert
        Assert.That(result[0].TotalDuration,
            Is.EqualTo(result[0].StopTime - result[0].StartTime));
    }
}
