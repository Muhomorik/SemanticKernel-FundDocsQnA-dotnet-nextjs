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
public class AboutFundPageDataCollector_InteractionSchedulingTests
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
    public void ScheduledStep_ActivateSekView_FiresAtCorrectTime()
    {
        // Arrange
        BeginDefaultCollection();

        // Act — first step fires at delay=0, need 1 tick to process
        _scheduler.AdvanceBy(1);

        // Assert
        _interactorMock.Verify(x => x.ActivateSekViewAsync(), Times.Once);
    }

    [Test]
    public void ScheduledStep_Select1Month_FiresAtCorrectTime()
    {
        // Arrange
        BeginDefaultCollection();
        var fireDelay = CollectionScheduleBuilder.DefaultDelays[AboutFundCollectionStepKind.ActivateSekView];

        // Act — advance to Select1Month fire time (cumulative: 30s)
        _scheduler.AdvanceBy(fireDelay.Ticks);

        // Assert
        _interactorMock.Verify(x => x.SelectPeriod1MonthAsync(), Times.Once);
    }

    [Test]
    public void ScheduledSteps_AllFireInCorrectOrder()
    {
        // Arrange
        var callOrder = new List<AboutFundCollectionStepKind>();
        _interactorMock.Setup(x => x.ActivateSekViewAsync())
            .Callback(() => callOrder.Add(AboutFundCollectionStepKind.ActivateSekView))
            .ReturnsAsync(true);
        _interactorMock.Setup(x => x.SelectPeriod1MonthAsync())
            .Callback(() => callOrder.Add(AboutFundCollectionStepKind.Select1Month))
            .ReturnsAsync(true);
        _interactorMock.Setup(x => x.SelectPeriod3MonthsAsync())
            .Callback(() => callOrder.Add(AboutFundCollectionStepKind.Select3Months))
            .ReturnsAsync(true);
        _interactorMock.Setup(x => x.SelectPeriodYearToDateAsync())
            .Callback(() => callOrder.Add(AboutFundCollectionStepKind.SelectYearToDate))
            .ReturnsAsync(true);
        _interactorMock.Setup(x => x.SelectPeriod1YearAsync())
            .Callback(() => callOrder.Add(AboutFundCollectionStepKind.Select1Year))
            .ReturnsAsync(true);
        _interactorMock.Setup(x => x.SelectPeriod3YearsAsync())
            .Callback(() => callOrder.Add(AboutFundCollectionStepKind.Select3Years))
            .ReturnsAsync(true);
        _interactorMock.Setup(x => x.SelectPeriod5YearsAsync())
            .Callback(() => callOrder.Add(AboutFundCollectionStepKind.Select5Years))
            .ReturnsAsync(true);
        _interactorMock.Setup(x => x.SelectPeriodMaxAsync())
            .Callback(() => callOrder.Add(AboutFundCollectionStepKind.SelectMax))
            .ReturnsAsync(true);

        BeginDefaultCollection();

        // Act — advance past all step fire times
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(110).Ticks);

        // Assert
        Assert.That(callOrder, Is.EqualTo(AboutFundCollectionStepKinds.All));
    }

    [Test]
    public void ScheduledStep_InteractionSucceeds_MarksStepCompleted()
    {
        // Arrange
        BeginDefaultCollection();
        var emitted = new List<AboutFundCollectionProgress>();
        _sut.StateChanged.Subscribe(emitted.Add);

        // Act — advance 1s so the step fires (at +0s) AND the 1s interval tick emits progress
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

        // Assert
        var activateStep = emitted.Last().Steps
            .First(s => s.Kind == AboutFundCollectionStepKind.ActivateSekView);
        Assert.That(activateStep.Status, Is.EqualTo(AboutFundCollectionStepStatus.Completed));
    }

    [Test]
    public void ScheduledStep_SelectMaxSucceeds_NextResponseTriggersCompletion()
    {
        // Arrange
        BeginDefaultCollection();
        var completed = new List<AboutFundPageData>();
        _sut.Completed.Subscribe(completed.Add);

        // Act — advance past all steps including SelectMax
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(110).Ticks);

        // Route a response while in draining phase
        var request = InterceptedRequestBuilder.ForSlot(AboutFundDataSlot.ChartMax)
            .WithResponseBody(_fixture.Create<string>())
            .Build();
        _sut.NotifyResponseCaptured(request);

        // Assert
        Assert.That(completed, Has.Count.EqualTo(1),
            "Completed should emit after SelectMax + response");
    }

    #region Helpers

    private void BeginDefaultCollection()
    {
        var schedule = new CollectionScheduleBuilder()
            .WithOrderBookId(_fixture.Create<OrderBookId>())
            .WithStartTime(_scheduler.Now)
            .WithAllDefaultSteps()
            .Build();
        _sut.BeginCollection(schedule);
    }

    #endregion
}
