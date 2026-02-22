using System.Reactive.Concurrency;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Reactive.Testing;
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
[TestOf(typeof(AboutFundPageDataCollector))]
public class AboutFundPageDataCollector_BeginCollectionTests
{
    private IFixture _fixture = null!;
    private TestScheduler _scheduler = null!;
    private Mock<IAboutFundPageInteractor> _interactorMock = null!;
    private AboutFundPageDataCollector _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());

        _scheduler = new TestScheduler();
        _fixture.Register<IScheduler>(() => _scheduler);
        _fixture.Register(() => TestEndpointPatterns.CreateDefault());

        _interactorMock = _fixture.Freeze<Mock<IAboutFundPageInteractor>>().SetupAllSucceed();
        _sut = _fixture.Create<AboutFundPageDataCollector>();
    }

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
    }

    [Test]
    public void BeginCollection_ValidSchedule_ReturnsProgressWithCorrectOrderBookId()
    {
        // Arrange
        var orderBookId = _fixture.Create<OrderBookId>();
        var schedule = CreateSchedule(orderBookId);

        // Act
        var progress = _sut.BeginCollection(schedule);

        // Assert
        Assert.That(progress.OrderBookId, Is.EqualTo(orderBookId));
    }

    [Test]
    public void BeginCollection_ValidSchedule_AllStepsArePending()
    {
        // Arrange
        var schedule = CreateSchedule();

        // Act
        var progress = _sut.BeginCollection(schedule);

        // Assert
        Assert.That(progress.Steps, Has.Count.EqualTo(AboutFundCollectionStepKinds.All.Count));
        Assert.That(progress.Steps.Select(s => s.Status),
            Is.All.EqualTo(AboutFundCollectionStepStatus.Pending));
    }

    [Test]
    public void BeginCollection_ValidSchedule_TotalDurationMatchesSchedule()
    {
        // Arrange
        var schedule = CreateSchedule();

        // Act
        var progress = _sut.BeginCollection(schedule);

        // Assert
        Assert.That(progress.TotalDuration, Is.EqualTo(schedule.TotalDuration));
    }

    [Test]
    public void BeginCollection_ValidSchedule_AllFetchSlotsArePending()
    {
        // Arrange
        var schedule = CreateSchedule();

        // Act
        var progress = _sut.BeginCollection(schedule);

        // Assert
        var pd = progress.PageData;
        Assert.That(pd.Chart1Month.Status, Is.EqualTo(AboutFundFetchStatus.Pending));
        Assert.That(pd.Chart3Months.Status, Is.EqualTo(AboutFundFetchStatus.Pending));
        Assert.That(pd.ChartYearToDate.Status, Is.EqualTo(AboutFundFetchStatus.Pending));
        Assert.That(pd.Chart1Year.Status, Is.EqualTo(AboutFundFetchStatus.Pending));
        Assert.That(pd.Chart3Years.Status, Is.EqualTo(AboutFundFetchStatus.Pending));
        Assert.That(pd.Chart5Years.Status, Is.EqualTo(AboutFundFetchStatus.Pending));
        Assert.That(pd.ChartMax.Status, Is.EqualTo(AboutFundFetchStatus.Pending));
    }

    [Test]
    public void BeginCollection_ValidSchedule_EmitsInitialProgressOnStateChanged()
    {
        // Arrange
        var schedule = CreateSchedule();
        var emitted = new List<AboutFundCollectionProgress>();
        _sut.StateChanged.Subscribe(emitted.Add);

        // Act
        _sut.BeginCollection(schedule);

        // Assert
        Assert.That(emitted, Has.Count.GreaterThanOrEqualTo(1),
            "StateChanged should emit at least one snapshot on BeginCollection");
        Assert.That(emitted[0].OrderBookId, Is.EqualTo(schedule.OrderBookId));
    }

    #region Helpers

    private AboutFundCollectionSchedule CreateSchedule(OrderBookId? orderBookId = null)
    {
        return new CollectionScheduleBuilder()
            .WithOrderBookId(orderBookId ?? _fixture.Create<OrderBookId>())
            .WithStartTime(_scheduler.Now)
            .WithAllDefaultSteps()
            .Build();
    }

    #endregion
}
