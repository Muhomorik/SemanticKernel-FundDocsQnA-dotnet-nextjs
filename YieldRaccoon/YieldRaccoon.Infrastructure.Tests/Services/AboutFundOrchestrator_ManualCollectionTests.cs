using System.Reactive.Subjects;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Moq;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Services;
using YieldRaccoon.Infrastructure.Tests.AutoFixture;
using IScheduler = System.Reactive.Concurrency.IScheduler;

namespace YieldRaccoon.Infrastructure.Tests.Services;

/// <summary>
/// Tests for <see cref="AboutFundOrchestrator.StartManualCollectionAsync"/>
/// and related manual collection mode behavior.
/// </summary>
[TestFixture]
[TestOf(typeof(AboutFundOrchestrator))]
public class AboutFundOrchestrator_ManualCollectionTests
{
    private IFixture _fixture = null!;
    private TestScheduler _scheduler = null!;

    private Mock<IFundDetailsUrlBuilder> _urlBuilderMock = null!;
    private Mock<IAboutFundPageDataCollector> _collectorMock = null!;
    private Mock<IAboutFundChartIngestionService> _ingestionMock = null!;
    private Mock<IFundProfileRepository> _repositoryMock = null!;
    private Mock<IAboutFundScheduleCalculator> _scheduleCalculatorMock = null!;

    // Subjects to control collector observable emissions
    private Subject<AboutFundPageData> _completedSubject = null!;
    private Subject<AboutFundCollectionProgress> _stateChangedSubject = null!;
    private Subject<AboutFundPageData> _slotUpdatedSubject = null!;

    private AboutFundOrchestrator _sut = null!;

    // Observables captured from orchestrator
    private List<AboutFundSessionState> _sessionStates = null!;
    private List<Uri> _navigateToUrls = null!;

    private static readonly Uri TestFundUrl = new("https://www.example.com/fonder/325410/about");
    private static readonly OrderBookId TestOrderBookId = OrderBookId.Create("325410");
    private const string TestIsin = "SE0000740698";
    private const string TestFundName = "Test Fund Alpha";

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());

        _scheduler = new TestScheduler();
        _fixture.Register<IScheduler>(() => _scheduler);

        // Set up collector observables BEFORE SUT creation (constructor subscribes)
        _completedSubject = new Subject<AboutFundPageData>();
        _stateChangedSubject = new Subject<AboutFundCollectionProgress>();
        _slotUpdatedSubject = new Subject<AboutFundPageData>();

        _collectorMock = _fixture.Freeze<Mock<IAboutFundPageDataCollector>>();
        _collectorMock.Setup(x => x.Completed).Returns(_completedSubject);
        _collectorMock.Setup(x => x.StateChanged).Returns(_stateChangedSubject);
        _collectorMock.Setup(x => x.SlotUpdated).Returns(_slotUpdatedSubject);

        _urlBuilderMock = _fixture.Freeze<Mock<IFundDetailsUrlBuilder>>();
        _repositoryMock = _fixture.Freeze<Mock<IFundProfileRepository>>();
        _ingestionMock = _fixture.Freeze<Mock<IAboutFundChartIngestionService>>();
        _scheduleCalculatorMock = _fixture.Freeze<Mock<IAboutFundScheduleCalculator>>();

        // Default mock setups
        _ingestionMock
            .Setup(x => x.IngestChartDataAsync(It.IsAny<AboutFundPageData>(), It.IsAny<IsinId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _repositoryMock
            .Setup(x => x.UpdateLastVisitedAtAsync(It.IsAny<IsinId>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = _fixture.Create<AboutFundOrchestrator>();

        // Subscribe to orchestrator observables
        _sessionStates = [];
        _navigateToUrls = [];
        _sut.SessionState.Subscribe(_sessionStates.Add);
        _sut.NavigateToUrl.Subscribe(_navigateToUrls.Add);
    }

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
        _completedSubject.Dispose();
        _stateChangedSubject.Dispose();
        _slotUpdatedSubject.Dispose();
    }

    #region StartManualCollectionAsync — Happy Path

    [Test]
    public async Task StartManualCollectionAsync_ValidUrl_ParsesOrderBookIdAndStartsPassiveCollection()
    {
        // Arrange
        SetupUrlParser(success: true);
        SetupIsinLookup(TestIsin);

        // Act
        await _sut.StartManualCollectionAsync(TestFundUrl);

        // Assert
        _collectorMock.Verify(x => x.BeginPassiveCollection(TestOrderBookId), Times.Once);
    }

    [Test]
    public async Task StartManualCollectionAsync_ValidUrl_EmitsNavigateToUrl()
    {
        // Arrange
        SetupUrlParser(success: true);
        SetupIsinLookup(TestIsin);

        // Act
        await _sut.StartManualCollectionAsync(TestFundUrl);

        // Assert
        Assert.That(_navigateToUrls, Has.Count.EqualTo(1));
        Assert.That(_navigateToUrls[0], Is.EqualTo(TestFundUrl));
    }

    [Test]
    public async Task StartManualCollectionAsync_ValidUrl_SetsPhaseToManualCollecting()
    {
        // Arrange
        SetupUrlParser(success: true);
        SetupIsinLookup(TestIsin);

        // Act
        await _sut.StartManualCollectionAsync(TestFundUrl);

        // Assert
        var latestState = _sessionStates[^1];
        Assert.That(latestState.Phase, Is.EqualTo(AboutFundSessionPhase.ManualCollecting));
    }

    [Test]
    public async Task StartManualCollectionAsync_ValidUrl_EmitsActiveSessionState()
    {
        // Arrange
        SetupUrlParser(success: true);
        SetupIsinLookup(TestIsin);

        // Act
        await _sut.StartManualCollectionAsync(TestFundUrl);

        // Assert
        var latestState = _sessionStates[^1];
        Assert.That(latestState.IsActive, Is.True);
        Assert.That(latestState.CurrentOrderBookId, Is.EqualTo(TestOrderBookId));
    }

    [Test]
    public async Task StartManualCollectionAsync_ValidUrl_SessionStateContainsIsin()
    {
        // Arrange
        SetupUrlParser(success: true);
        SetupIsinLookup(TestIsin);

        // Act
        await _sut.StartManualCollectionAsync(TestFundUrl);

        // Assert
        var latestState = _sessionStates[^1];
        Assert.That(latestState.CurrentIsin, Is.EqualTo(TestIsin));
    }

    [Test]
    public async Task StartManualCollectionAsync_FundInSchedule_SessionStateContainsFundName()
    {
        // Arrange
        SetupUrlParser(success: true);
        SetupScheduleWithFund(TestOrderBookId, TestIsin, TestFundName);

        // Act
        await _sut.LoadScheduleAsync();
        await _sut.StartManualCollectionAsync(TestFundUrl);

        // Assert
        var latestState = _sessionStates[^1];
        Assert.That(latestState.CurrentFundName, Is.EqualTo(TestFundName));
    }

    #endregion

    #region StartManualCollectionAsync — URL parsing failures

    [Test]
    public async Task StartManualCollectionAsync_UnrecognizedUrl_StillEmitsNavigateToUrl()
    {
        // Arrange
        SetupUrlParser(success: false);

        // Act
        await _sut.StartManualCollectionAsync(TestFundUrl);

        // Assert
        Assert.That(_navigateToUrls, Has.Count.EqualTo(1));
        Assert.That(_navigateToUrls[0], Is.EqualTo(TestFundUrl));
    }

    [Test]
    public async Task StartManualCollectionAsync_UnrecognizedUrl_DoesNotStartCollection()
    {
        // Arrange
        SetupUrlParser(success: false);

        // Act
        await _sut.StartManualCollectionAsync(TestFundUrl);

        // Assert
        _collectorMock.Verify(x => x.BeginPassiveCollection(It.IsAny<OrderBookId>()), Times.Never);
    }

    [Test]
    public async Task StartManualCollectionAsync_UnrecognizedUrl_PhaseRemainsIdle()
    {
        // Arrange
        SetupUrlParser(success: false);

        // Act
        await _sut.StartManualCollectionAsync(TestFundUrl);

        // Assert
        var latestState = _sessionStates[^1];
        Assert.That(latestState.Phase, Is.EqualTo(AboutFundSessionPhase.Idle));
    }

    #endregion

    #region StartManualCollectionAsync — ISIN not found

    [Test]
    public async Task StartManualCollectionAsync_IsinNotFound_NavigatesWithoutStartingCollection()
    {
        // Arrange
        SetupUrlParser(success: true);
        SetupIsinLookup(null); // Not in DB

        // Act
        await _sut.StartManualCollectionAsync(TestFundUrl);

        // Assert — still navigates
        Assert.That(_navigateToUrls, Has.Count.EqualTo(1));

        // But no passive collection started
        _collectorMock.Verify(x => x.BeginPassiveCollection(It.IsAny<OrderBookId>()), Times.Never);
    }

    #endregion

    #region StartManualCollectionAsync — Automated session active

    [Test]
    public async Task StartManualCollectionAsync_AutomatedSessionActive_NavigatesOnly()
    {
        // Arrange — start an automated session
        await StartAutomatedSession();

        SetupUrlParser(success: true);
        SetupIsinLookup(TestIsin);

        // Act
        await _sut.StartManualCollectionAsync(TestFundUrl);

        // Assert — navigates but doesn't start passive collection
        Assert.That(_navigateToUrls.Any(u => u == TestFundUrl), Is.True);
        _collectorMock.Verify(x => x.BeginPassiveCollection(It.IsAny<OrderBookId>()), Times.Never);
    }

    #endregion

    #region Per-slot persistence

    [Test]
    public async Task StartManualCollectionAsync_SlotResolved_PersistsChartDataViaIngestionService()
    {
        // Arrange
        SetupUrlParser(success: true);
        SetupIsinLookup(TestIsin);
        await _sut.StartManualCollectionAsync(TestFundUrl);

        var pageData = new AboutFundPageData
        {
            OrderBookId = TestOrderBookId,
            Chart1Month = AboutFundFetchSlot.Succeeded("chart-data")
        };

        // Act — simulate a slot resolving
        _slotUpdatedSubject.OnNext(pageData);

        // Allow fire-and-forget async to complete
        await Task.Delay(50);

        // Assert
        _ingestionMock.Verify(
            x => x.IngestChartDataAsync(pageData, IsinId.Create(TestIsin), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task StartManualCollectionAsync_SlotResolved_UpdatesLastVisitedAt()
    {
        // Arrange
        SetupUrlParser(success: true);
        SetupIsinLookup(TestIsin);
        await _sut.StartManualCollectionAsync(TestFundUrl);

        var pageData = new AboutFundPageData { OrderBookId = TestOrderBookId };

        // Act
        _slotUpdatedSubject.OnNext(pageData);
        await Task.Delay(50);

        // Assert
        _repositoryMock.Verify(
            x => x.UpdateLastVisitedAtAsync(IsinId.Create(TestIsin), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task StartManualCollectionAsync_MultipleSlots_PersistsAfterEach()
    {
        // Arrange
        SetupUrlParser(success: true);
        SetupIsinLookup(TestIsin);
        await _sut.StartManualCollectionAsync(TestFundUrl);

        var pageData1 = new AboutFundPageData
        {
            OrderBookId = TestOrderBookId,
            Chart1Month = AboutFundFetchSlot.Succeeded("data-1")
        };
        var pageData2 = pageData1 with
        {
            Chart3Months = AboutFundFetchSlot.Succeeded("data-2")
        };

        // Act — simulate 2 slots resolving
        _slotUpdatedSubject.OnNext(pageData1);
        _slotUpdatedSubject.OnNext(pageData2);
        await Task.Delay(50);

        // Assert — ingestion called twice
        _ingestionMock.Verify(
            x => x.IngestChartDataAsync(It.IsAny<AboutFundPageData>(), IsinId.Create(TestIsin), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    #endregion

    #region Transitions

    [Test]
    public async Task StartManualCollectionAsync_NewUrl_SilentlyTransitionsToPreviousCollection()
    {
        // Arrange — start manual collection for first URL
        SetupUrlParser(success: true);
        SetupIsinLookup(TestIsin);
        await _sut.StartManualCollectionAsync(TestFundUrl);

        // Second URL setup
        var secondUrl = new Uri("https://www.example.com/fonder/999999/about");
        var secondOrderBookId = OrderBookId.Create("999999");
        _urlBuilderMock
            .Setup(x => x.TryParseOrderBookId(secondUrl, out secondOrderBookId))
            .Returns(true);
        _repositoryMock
            .Setup(x => x.GetIsinByOrderBookIdAsync(secondOrderBookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestIsin);

        // Act — navigate to a different fund
        await _sut.StartManualCollectionAsync(secondUrl);

        // Assert — second passive collection started, previous was cleaned up
        _collectorMock.Verify(x => x.BeginPassiveCollection(secondOrderBookId), Times.Once);
        Assert.That(_navigateToUrls[^1], Is.EqualTo(secondUrl));
    }

    [Test]
    public async Task CancelSession_DuringManualMode_ResetsToIdle()
    {
        // Arrange
        SetupUrlParser(success: true);
        SetupIsinLookup(TestIsin);
        await _sut.StartManualCollectionAsync(TestFundUrl);

        // Act
        _sut.CancelSession("User stopped");

        // Assert
        var latestState = _sessionStates[^1];
        Assert.That(latestState.IsActive, Is.False);
        Assert.That(latestState.Phase, Is.EqualTo(AboutFundSessionPhase.Idle));
    }

    [Test]
    public async Task CancelSession_DuringManualMode_CancelsCollectorCollection()
    {
        // Arrange
        SetupUrlParser(success: true);
        SetupIsinLookup(TestIsin);
        await _sut.StartManualCollectionAsync(TestFundUrl);

        // Act
        _sut.CancelSession("User stopped");

        // Assert
        _collectorMock.Verify(x => x.CancelCollection(), Times.Once);
    }

    [Test]
    public async Task StartSessionAsync_DuringManualMode_CancelsManualAndStartsAutomated()
    {
        // Arrange — start manual collection
        SetupUrlParser(success: true);
        SetupIsinLookup(TestIsin);
        await _sut.StartManualCollectionAsync(TestFundUrl);

        // Setup automated session prerequisites
        SetupScheduleForAutomatedSession();

        // Act — start automated session (should cancel manual first)
        await _sut.StartSessionAsync();

        // Assert — manual collection was cancelled, automated session started
        var latestState = _sessionStates[^1];
        Assert.That(latestState.Phase, Is.Not.EqualTo(AboutFundSessionPhase.ManualCollecting));
        Assert.That(latestState.SessionId, Is.Not.Null);
    }

    #endregion

    #region Helpers

    private void SetupUrlParser(bool success)
    {
        var orderBookId = TestOrderBookId;
        _urlBuilderMock
            .Setup(x => x.TryParseOrderBookId(TestFundUrl, out orderBookId))
            .Returns(success);
    }

    private void SetupIsinLookup(string? isin)
    {
        _repositoryMock
            .Setup(x => x.GetIsinByOrderBookIdAsync(TestOrderBookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(isin);
    }

    private void SetupScheduleWithFund(OrderBookId orderBookId, string isin, string name)
    {
        var scheduleItems = new List<AboutFundScheduleItem>
        {
            new()
            {
                OrderBookId = orderBookId,
                Isin = isin,
                Name = name,
                HistoryRecordCount = 0
            }
        };

        _repositoryMock
            .Setup(x => x.GetFundsOrderedByLastVisitAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleItems);
    }

    private void SetupScheduleForAutomatedSession()
    {
        var orderBookId = _fixture.Create<OrderBookId>();
        var scheduleItems = new List<AboutFundScheduleItem>
        {
            new()
            {
                OrderBookId = orderBookId,
                Isin = _fixture.Create<string>(),
                Name = "Automated Fund",
                HistoryRecordCount = 0
            }
        };

        _repositoryMock
            .Setup(x => x.GetFundsOrderedByLastVisitAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleItems);

        // Schedule calculator needs to return schedules for StartSessionAsync
        _scheduleCalculatorMock
            .Setup(x => x.CalculateSessionSchedule(
                It.IsAny<IReadOnlyList<AboutFundScheduleItem>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<Func<AboutFundCollectionStepKind, TimeSpan>>(),
                It.IsAny<IReadOnlyList<AboutFundCollectionStepKind>>()))
            .Returns(new List<AboutFundCollectionSchedule>
            {
                new()
                {
                    OrderBookId = orderBookId,
                    StartTime = _scheduler.Now + TimeSpan.FromSeconds(15),
                    StopTime = _scheduler.Now + TimeSpan.FromSeconds(120),
                    Steps = [],
                    InterPageDelay = TimeSpan.FromSeconds(5)
                }
            });

        _urlBuilderMock
            .Setup(x => x.BuildUrl(orderBookId))
            .Returns(new Uri("https://www.example.com/fonder/auto/about"));
    }

    private async Task StartAutomatedSession()
    {
        SetupScheduleForAutomatedSession();
        await _sut.StartSessionAsync();
    }

    #endregion
}
