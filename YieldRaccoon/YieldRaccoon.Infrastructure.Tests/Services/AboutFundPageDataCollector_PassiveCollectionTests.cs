using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Moq;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Services;
using YieldRaccoon.Infrastructure.Tests.AutoFixture;
using YieldRaccoon.Infrastructure.Tests.TestHelpers;
using IScheduler = System.Reactive.Concurrency.IScheduler;

namespace YieldRaccoon.Infrastructure.Tests.Services;

/// <summary>
/// Tests for <see cref="AboutFundPageDataCollector.BeginPassiveCollection"/>
/// and <see cref="AboutFundPageDataCollector.SlotUpdated"/>.
/// </summary>
[TestFixture]
[TestOf(typeof(AboutFundPageDataCollector))]
public class AboutFundPageDataCollector_PassiveCollectionTests
{
    private IFixture _fixture = null!;
    private TestScheduler _scheduler = null!;
    private Mock<IAboutFundPageInteractor> _interactorMock = null!;
    private AboutFundPageDataCollector _sut = null!;

    /// <summary>
    /// Captures all <see cref="AboutFundCollectionProgress"/> emissions.
    /// Subscribed before any action so initial emissions are captured.
    /// </summary>
    private List<AboutFundCollectionProgress> _progressEmissions = null!;

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

        // Subscribe early so initial emissions from BeginPassiveCollection are captured
        _progressEmissions = [];
        _sut.StateChanged.Subscribe(_progressEmissions.Add);
    }

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
    }

    #region BeginPassiveCollection

    [Test]
    public void BeginPassiveCollection_EmitsInitialProgress_WithCorrectOrderBookId()
    {
        // Arrange
        var orderBookId = _fixture.Create<OrderBookId>();

        // Act
        _sut.BeginPassiveCollection(orderBookId);

        // Assert
        Assert.That(_progressEmissions, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(LatestProgress.PageData.OrderBookId, Is.EqualTo(orderBookId));
    }

    [Test]
    public void BeginPassiveCollection_DoesNotScheduleAnyTimers()
    {
        // Arrange
        var orderBookId = _fixture.Create<OrderBookId>();
        _sut.BeginPassiveCollection(orderBookId);

        // Act — advance well past where any interaction timers would fire
        _scheduler.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);

        // Assert — no page interactions should have been called
        _interactorMock.Verify(x => x.ActivateSekViewAsync(), Times.Never);
        _interactorMock.Verify(x => x.SelectPeriod1MonthAsync(), Times.Never);
        _interactorMock.Verify(x => x.SelectPeriodMaxAsync(), Times.Never);
    }

    [Test]
    public void BeginPassiveCollection_PreviousActiveCollection_ForceCompletesAndEmitsOnCompleted()
    {
        // Arrange — start an active collection first
        var firstOrderBookId = _fixture.Create<OrderBookId>();
        var schedule = new CollectionScheduleBuilder()
            .WithOrderBookId(firstOrderBookId)
            .WithStartTime(_scheduler.Now)
            .WithAllDefaultSteps()
            .Build();
        _sut.BeginCollection(schedule);

        var completed = new List<AboutFundPageData>();
        _sut.Completed.Subscribe(completed.Add);

        // Act — start passive collection, which should force-complete the previous one
        var secondOrderBookId = _fixture.Create<OrderBookId>();
        _sut.BeginPassiveCollection(secondOrderBookId);

        // Assert
        Assert.That(completed, Has.Count.EqualTo(1));
        Assert.That(completed[0].OrderBookId, Is.EqualTo(firstOrderBookId));
    }

    [Test]
    public void BeginPassiveCollection_AllSlotsStartAsPending()
    {
        // Arrange & Act
        _sut.BeginPassiveCollection(_fixture.Create<OrderBookId>());

        // Assert
        var pageData = LatestProgress.PageData;

        Assert.That(pageData.Chart1Month.Status, Is.EqualTo(AboutFundFetchStatus.Pending));
        Assert.That(pageData.Chart3Months.Status, Is.EqualTo(AboutFundFetchStatus.Pending));
        Assert.That(pageData.ChartYearToDate.Status, Is.EqualTo(AboutFundFetchStatus.Pending));
        Assert.That(pageData.Chart1Year.Status, Is.EqualTo(AboutFundFetchStatus.Pending));
        Assert.That(pageData.Chart3Years.Status, Is.EqualTo(AboutFundFetchStatus.Pending));
        Assert.That(pageData.Chart5Years.Status, Is.EqualTo(AboutFundFetchStatus.Pending));
        Assert.That(pageData.ChartMax.Status, Is.EqualTo(AboutFundFetchStatus.Pending));
    }

    [Test]
    public void BeginPassiveCollection_StepsAreEmpty()
    {
        // Arrange & Act
        _sut.BeginPassiveCollection(_fixture.Create<OrderBookId>());

        // Assert
        Assert.That(LatestProgress.Steps, Is.Empty);
    }

    #endregion

    #region Response routing in passive mode

    [Test]
    public void PassiveCollection_MatchedResponse_SetsSlotSucceeded()
    {
        // Arrange
        _sut.BeginPassiveCollection(_fixture.Create<OrderBookId>());

        var responseBody = _fixture.Create<string>();
        var request = InterceptedRequestBuilder.ForSlot(AboutFundDataSlot.Chart1Month)
            .WithResponseBody(responseBody)
            .Build();

        // Act
        _sut.NotifyResponseCaptured(request);

        // Assert
        Assert.That(LatestProgress.PageData.Chart1Month.Status, Is.EqualTo(AboutFundFetchStatus.Succeeded));
        Assert.That(LatestProgress.PageData.Chart1Month.Data, Is.EqualTo(responseBody));
    }

    [Test]
    public void PassiveCollection_UnmatchedResponse_IsIgnored()
    {
        // Arrange
        _sut.BeginPassiveCollection(_fixture.Create<OrderBookId>());

        // Act
        _sut.NotifyResponseCaptured(InterceptedRequestBuilder.Unmatched().Build());

        // Assert — all slots still pending
        Assert.That(LatestProgress.PageData.ResolvedCount, Is.EqualTo(0));
    }

    #endregion

    #region SlotUpdated observable

    [Test]
    public void SlotUpdated_EmitsPageDataSnapshot_WhenSlotTransitionsFromPending()
    {
        // Arrange
        var orderBookId = _fixture.Create<OrderBookId>();
        _sut.BeginPassiveCollection(orderBookId);

        var slotUpdates = new List<AboutFundPageData>();
        _sut.SlotUpdated.Subscribe(slotUpdates.Add);

        // Act
        _sut.NotifyResponseCaptured(
            InterceptedRequestBuilder.ForSlot(AboutFundDataSlot.Chart1Month)
                .WithResponseBody(_fixture.Create<string>())
                .Build());

        // Assert
        Assert.That(slotUpdates, Has.Count.EqualTo(1));
        Assert.That(slotUpdates[0].OrderBookId, Is.EqualTo(orderBookId));
        Assert.That(slotUpdates[0].Chart1Month.Status, Is.EqualTo(AboutFundFetchStatus.Succeeded));
    }

    [Test]
    public void SlotUpdated_DoesNotEmit_WhenSlotAlreadyResolved()
    {
        // Arrange
        _sut.BeginPassiveCollection(_fixture.Create<OrderBookId>());

        var slotUpdates = new List<AboutFundPageData>();
        _sut.SlotUpdated.Subscribe(slotUpdates.Add);

        // First response resolves the slot
        _sut.NotifyResponseCaptured(
            InterceptedRequestBuilder.ForSlot(AboutFundDataSlot.Chart1Month)
                .WithResponseBody("first")
                .Build());

        // Act — second response for the same slot
        _sut.NotifyResponseCaptured(
            InterceptedRequestBuilder.ForSlot(AboutFundDataSlot.Chart1Month)
                .WithResponseBody("second")
                .Build());

        // Assert — only one emission (the initial transition)
        Assert.That(slotUpdates, Has.Count.EqualTo(1));
    }

    [Test]
    public void SlotUpdated_EmitsForEachSlot_IndependentlyAsTheyResolve()
    {
        // Arrange
        _sut.BeginPassiveCollection(_fixture.Create<OrderBookId>());

        var slotUpdates = new List<AboutFundPageData>();
        _sut.SlotUpdated.Subscribe(slotUpdates.Add);

        // Act — resolve 3 different slots
        _sut.NotifyResponseCaptured(
            InterceptedRequestBuilder.ForSlot(AboutFundDataSlot.Chart1Month)
                .WithResponseBody(_fixture.Create<string>())
                .Build());
        _sut.NotifyResponseCaptured(
            InterceptedRequestBuilder.ForSlot(AboutFundDataSlot.Chart3Years)
                .WithResponseBody(_fixture.Create<string>())
                .Build());
        _sut.NotifyResponseCaptured(
            InterceptedRequestBuilder.ForSlot(AboutFundDataSlot.ChartMax)
                .WithResponseBody(_fixture.Create<string>())
                .Build());

        // Assert
        Assert.That(slotUpdates, Has.Count.EqualTo(3));

        // Each snapshot should show cumulative state
        Assert.That(slotUpdates[0].ResolvedCount, Is.EqualTo(1));
        Assert.That(slotUpdates[1].ResolvedCount, Is.EqualTo(2));
        Assert.That(slotUpdates[2].ResolvedCount, Is.EqualTo(3));
    }

    [Test]
    public void SlotUpdated_FailedSlot_AlsoEmits()
    {
        // Arrange
        _sut.BeginPassiveCollection(_fixture.Create<OrderBookId>());

        var slotUpdates = new List<AboutFundPageData>();
        _sut.SlotUpdated.Subscribe(slotUpdates.Add);

        // Act — route a failed response (HTTP 500)
        _sut.NotifyResponseCaptured(
            InterceptedRequestBuilder.ForSlot(AboutFundDataSlot.Chart1Month)
                .WithStatusCode(500, "Internal Server Error")
                .Build());

        // Assert
        Assert.That(slotUpdates, Has.Count.EqualTo(1));
        Assert.That(slotUpdates[0].Chart1Month.Status, Is.EqualTo(AboutFundFetchStatus.Failed));
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Returns the most recent <see cref="AboutFundCollectionProgress"/> emission.
    /// </summary>
    private AboutFundCollectionProgress LatestProgress => _progressEmissions[^1];

    #endregion
}
