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
public class AboutFundPageDataCollector_CompletionTests
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
    public void Completion_DrainingAndResponseArrives_EmitsCompletedWithCorrectOrderBookId()
    {
        // Arrange
        var orderBookId = _fixture.Create<OrderBookId>();
        var schedule = CreateSchedule(orderBookId);
        _sut.BeginCollection(schedule);
        var completed = new List<AboutFundPageData>();
        _sut.Completed.Subscribe(completed.Add);

        // Act — advance past all steps to enter Draining
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(110).Ticks);

        // Route the final response to trigger completion
        _sut.NotifyResponseCaptured(
            InterceptedRequestBuilder.ForSlot(AboutFundDataSlot.ChartMax)
                .WithResponseBody(_fixture.Create<string>())
                .Build());

        // Assert
        Assert.That(completed, Has.Count.EqualTo(1));
        Assert.That(completed[0].OrderBookId, Is.EqualTo(orderBookId));
    }

    [Test]
    public void Completion_AllSlotsSucceeded_IsFullySuccessfulIsTrue()
    {
        // Arrange
        var schedule = CreateSchedule();
        _sut.BeginCollection(schedule);
        var completed = new List<AboutFundPageData>();
        _sut.Completed.Subscribe(completed.Add);

        // Route all 7 slot responses
        foreach (AboutFundDataSlot slot in Enum.GetValues<AboutFundDataSlot>())
        {
            _sut.NotifyResponseCaptured(
                InterceptedRequestBuilder.ForSlot(slot)
                    .WithResponseBody(_fixture.Create<string>())
                    .Build());
        }

        // Advance past all steps so SelectMax fires and enters Draining
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(110).Ticks);

        // Route one more response to trigger completion from Draining
        _sut.NotifyResponseCaptured(
            InterceptedRequestBuilder.ForSlot(AboutFundDataSlot.ChartMax)
                .WithResponseBody(_fixture.Create<string>())
                .Build());

        // Assert
        Assert.That(completed, Has.Count.EqualTo(1));
        Assert.That(completed[0].IsFullySuccessful, Is.True,
            "All slots should be succeeded when all responses arrived with 200");
    }

    [Test]
    public void Completion_SafetyNetTimer_ForcesCompletionAtTotalDuration()
    {
        // Arrange
        var schedule = CreateSchedule();
        _sut.BeginCollection(schedule);
        var completed = new List<AboutFundPageData>();
        _sut.Completed.Subscribe(completed.Add);

        // Act — advance past total duration (safety-net fires)
        _scheduler.AdvanceBy(schedule.TotalDuration.Ticks);

        // Assert
        Assert.That(completed, Has.Count.EqualTo(1),
            "Safety-net timer should force completion at TotalDuration");
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
