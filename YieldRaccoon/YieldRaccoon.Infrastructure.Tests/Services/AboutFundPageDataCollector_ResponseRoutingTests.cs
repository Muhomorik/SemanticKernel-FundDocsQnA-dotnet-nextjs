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
public class AboutFundPageDataCollector_ResponseRoutingTests
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
    public void NotifyResponseCaptured_MatchingUrl_SetsSlotSucceeded()
    {
        // Arrange
        BeginDefaultCollection();
        var responseBody = _fixture.Create<string>();
        var request = InterceptedRequestBuilder.ForSlot(AboutFundDataSlot.Chart1Month)
            .WithResponseBody(responseBody)
            .Build();

        // Act
        _sut.NotifyResponseCaptured(request);

        // Assert
        var progress = CaptureLatestProgress();
        Assert.That(progress.PageData.Chart1Month.Status, Is.EqualTo(AboutFundFetchStatus.Succeeded));
        Assert.That(progress.PageData.Chart1Month.Data, Is.EqualTo(responseBody));
    }

    [Test]
    public void NotifyResponseCaptured_UnmatchedUrl_NoSlotChanges()
    {
        // Arrange
        BeginDefaultCollection();
        var request = InterceptedRequestBuilder.Unmatched().Build();

        // Act
        _sut.NotifyResponseCaptured(request);

        // Assert
        var progress = CaptureLatestProgress();
        Assert.That(progress.PageData.ResolvedCount, Is.EqualTo(0),
            "No slots should be resolved for an unmatched URL");
    }

    [Test]
    public void NotifyResponseCaptured_MatchingUrl_EmitsProgressOnStateChanged()
    {
        // Arrange
        BeginDefaultCollection();
        var emitted = new List<AboutFundCollectionProgress>();
        _sut.StateChanged.Subscribe(emitted.Add);
        var request = InterceptedRequestBuilder.ForSlot(AboutFundDataSlot.Chart3Months)
            .WithResponseBody(_fixture.Create<string>())
            .Build();

        // Act
        _sut.NotifyResponseCaptured(request);

        // Assert
        Assert.That(emitted, Has.Count.GreaterThanOrEqualTo(1),
            "StateChanged should emit after a slot update");
        Assert.That(emitted.Last().PageData.Chart3Months.Status,
            Is.EqualTo(AboutFundFetchStatus.Succeeded));
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

    private AboutFundCollectionProgress CaptureLatestProgress()
    {
        AboutFundCollectionProgress? latest = null;
        _sut.StateChanged.Subscribe(p => latest = p);
        // Trigger a progress tick so we get the latest state
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
        return latest!;
    }

    #endregion
}
