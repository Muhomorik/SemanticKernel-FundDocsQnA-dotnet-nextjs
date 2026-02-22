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
public class AboutFundPageDataCollector_ProgressReportingTests
{
    private IFixture _fixture = null!;
    private TestScheduler _scheduler = null!;
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

        _fixture.Freeze<Mock<IAboutFundPageInteractor>>().SetupAllSucceed();
        _sut = _fixture.Create<AboutFundPageDataCollector>();
    }

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
    }

    [Test]
    public void Progress_OneSecondTick_EmitsWithCorrectElapsedAndRemaining()
    {
        // Arrange
        var schedule = CreateSchedule();
        _sut.BeginCollection(schedule);
        var emitted = new List<AboutFundCollectionProgress>();
        _sut.StateChanged.Subscribe(emitted.Add);

        // Act — advance 1 second
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

        // Assert
        var tickProgress = emitted.Last();
        Assert.That(tickProgress.Elapsed, Is.EqualTo(TimeSpan.FromSeconds(1)));
        Assert.That(tickProgress.Remaining,
            Is.EqualTo(schedule.TotalDuration - TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void Progress_AfterSlotUpdate_ContainsUpdatedPageData()
    {
        // Arrange
        var schedule = CreateSchedule();
        _sut.BeginCollection(schedule);
        var responseBody = _fixture.Create<string>();

        // Route a response to fill a slot
        _sut.NotifyResponseCaptured(
            InterceptedRequestBuilder.ForSlot(AboutFundDataSlot.Chart1Year)
                .WithResponseBody(responseBody)
                .Build());

        var emitted = new List<AboutFundCollectionProgress>();
        _sut.StateChanged.Subscribe(emitted.Add);

        // Act — advance to get a progress tick
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

        // Assert
        var latest = emitted.Last();
        Assert.That(latest.PageData.Chart1Year.Status, Is.EqualTo(AboutFundFetchStatus.Succeeded));
        Assert.That(latest.PageData.Chart1Year.Data, Is.EqualTo(responseBody));
    }

    [Test]
    public void Progress_AfterStepFires_ContainsUpdatedStepStatus()
    {
        // Arrange
        var schedule = CreateSchedule();
        _sut.BeginCollection(schedule);
        var emitted = new List<AboutFundCollectionProgress>();
        _sut.StateChanged.Subscribe(emitted.Add);

        // Act — advance past ActivateSekView fire time (fires at +0s)
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

        // Assert
        var latest = emitted.Last();
        var activateStep = latest.Steps
            .First(s => s.Kind == AboutFundCollectionStepKind.ActivateSekView);
        Assert.That(activateStep.Status, Is.EqualTo(AboutFundCollectionStepStatus.Completed));
    }

    #region Helpers

    private AboutFundCollectionSchedule CreateSchedule()
    {
        return new CollectionScheduleBuilder()
            .WithOrderBookId(_fixture.Create<OrderBookId>())
            .WithStartTime(_scheduler.Now)
            .WithAllDefaultSteps()
            .Build();
    }

    #endregion
}
