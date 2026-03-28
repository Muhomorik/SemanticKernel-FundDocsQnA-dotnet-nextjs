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
public class AboutFundPageDataCollector_FundReferenceTests
{
    private static readonly string FixturePath = Path.Combine(
        AppContext.BaseDirectory, "TestData", "fund-reference-response.json");

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
    public void NotifyResponseCaptured_FundReferenceUrl_StoresFundReferenceJson()
    {
        // Arrange
        BeginDefaultCollection();
        var json = File.ReadAllText(FixturePath);
        var request = new InterceptedRequestBuilder()
            .WithUrl("https://www.avanza.se/_api/fund-reference/reference/1432959")
            .WithResponseBody(json)
            .WithStatusCode(200)
            .Build();

        // Act
        _sut.NotifyResponseCaptured(request);

        // Assert
        var progress = CaptureLatestProgress();
        Assert.That(progress.PageData.FundReferenceJson, Is.EqualTo(json));
    }

    [Test]
    public void NotifyResponseCaptured_NonFundReferenceUrl_DoesNotSetFundReferenceJson()
    {
        // Arrange
        BeginDefaultCollection();
        var request = InterceptedRequestBuilder.Unmatched()
            .WithResponseBody("""{ "data": "test" }""")
            .Build();

        // Act
        _sut.NotifyResponseCaptured(request);

        // Assert
        var progress = CaptureLatestProgress();
        Assert.That(progress.PageData.FundReferenceJson, Is.Null);
    }

    [Test]
    public void NotifyResponseCaptured_FundReferenceUrl_DoesNotAffectSlotCompletion()
    {
        // Arrange
        BeginDefaultCollection();
        var request = new InterceptedRequestBuilder()
            .WithUrl("https://www.avanza.se/_api/fund-reference/reference/12345")
            .WithResponseBody("""{ "description": "test" }""")
            .WithStatusCode(200)
            .Build();

        // Act
        _sut.NotifyResponseCaptured(request);

        // Assert
        var progress = CaptureLatestProgress();
        Assert.Multiple(() =>
        {
            Assert.That(progress.PageData.IsComplete, Is.False,
                "FundReference capture must not affect slot completion");
            Assert.That(progress.PageData.ResolvedCount, Is.EqualTo(0),
                "FundReference capture must not count as a resolved slot");
            Assert.That(progress.PageData.TotalSlots, Is.EqualTo(7),
                "TotalSlots must remain 7 (chart slots only)");
        });
    }

    [Test]
    public void NotifyResponseCaptured_FundReferenceNon200_DoesNotStoreJson()
    {
        // Arrange
        BeginDefaultCollection();
        var request = new InterceptedRequestBuilder()
            .WithUrl("https://www.avanza.se/_api/fund-reference/reference/12345")
            .WithResponseBody("""{ "error": "not found" }""")
            .WithStatusCode(404, "Not Found")
            .Build();

        // Act
        _sut.NotifyResponseCaptured(request);

        // Assert
        var progress = CaptureLatestProgress();
        Assert.That(progress.PageData.FundReferenceJson, Is.Null);
    }

    [Test]
    public void NotifyResponseCaptured_FundReferenceAndChartSlot_BothCaptured()
    {
        // Arrange
        BeginDefaultCollection();
        var fundRefJson = """{ "description": "Fund description" }""";
        var chartJson = """{ "dataSerie": [{"x": 1000, "y": 100.5}] }""";

        var fundRefRequest = new InterceptedRequestBuilder()
            .WithUrl("https://www.avanza.se/_api/fund-reference/reference/12345")
            .WithResponseBody(fundRefJson)
            .WithStatusCode(200)
            .Build();

        var chartRequest = InterceptedRequestBuilder.ForSlot(AboutFundDataSlot.Chart1Month)
            .WithResponseBody(chartJson)
            .Build();

        // Act
        _sut.NotifyResponseCaptured(fundRefRequest);
        _sut.NotifyResponseCaptured(chartRequest);

        // Assert
        var progress = CaptureLatestProgress();
        Assert.Multiple(() =>
        {
            Assert.That(progress.PageData.FundReferenceJson, Is.EqualTo(fundRefJson),
                "Fund-reference JSON should be captured");
            Assert.That(progress.PageData.Chart1Month.IsSucceeded, Is.True,
                "Chart slot should also be captured independently");
        });
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
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
        return latest!;
    }

    #endregion
}
