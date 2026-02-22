using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NLog;
using NUnit.Framework;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Services;
using YieldRaccoon.Infrastructure.Tests.AutoFixture;

namespace YieldRaccoon.Infrastructure.Tests.Services;

[TestFixture]
[TestOf(typeof(AboutFundChartIngestionService))]
public class AboutFundChartIngestionService_IngestChartDataAsyncTests
{
    private IFixture _fixture = null!;
    private Mock<ILogger> _loggerMock = null!;
    private Mock<IFundHistoryRepository> _repositoryMock = null!;
    private AboutFundChartIngestionService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture()
            .Customize(new AutoMoqCustomization())
            .Customize(new YieldRaccoonCustomization());

        _loggerMock = _fixture.Freeze<Mock<ILogger>>();
        _repositoryMock = _fixture.Freeze<Mock<IFundHistoryRepository>>();
        _sut = _fixture.Create<AboutFundChartIngestionService>();
    }

    #region No data scenarios

    [Test]
    public async Task IngestChartDataAsync_AllSlotsPending_ReturnsZeroAndSkipsRepository()
    {
        // Arrange — all slots default to Pending
        var pageData = new AboutFundPageData
        {
            OrderBookId = _fixture.Create<OrderBookId>()
        };
        var isinId = _fixture.Create<IsinId>();

        // Act
        var result = await _sut.IngestChartDataAsync(pageData, isinId);

        // Assert
        Assert.That(result, Is.EqualTo(0));
        _repositoryMock.Verify(
            r => r.AddRangeIfNotExistsAsync(
                It.IsAny<IEnumerable<FundHistoryRecord>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task IngestChartDataAsync_AllSlotsFailed_ReturnsZeroAndSkipsRepository()
    {
        // Arrange
        var pageData = new AboutFundPageData
        {
            OrderBookId = _fixture.Create<OrderBookId>(),
            Chart1Month = AboutFundFetchSlot.Failed("timeout"),
            Chart3Months = AboutFundFetchSlot.Failed("timeout"),
            ChartYearToDate = AboutFundFetchSlot.Failed("timeout"),
            Chart1Year = AboutFundFetchSlot.Failed("timeout"),
            Chart3Years = AboutFundFetchSlot.Failed("timeout"),
            Chart5Years = AboutFundFetchSlot.Failed("timeout"),
            ChartMax = AboutFundFetchSlot.Failed("timeout"),
        };
        var isinId = _fixture.Create<IsinId>();

        // Act
        var result = await _sut.IngestChartDataAsync(pageData, isinId);

        // Assert
        Assert.That(result, Is.EqualTo(0));
        _repositoryMock.Verify(
            r => r.AddRangeIfNotExistsAsync(
                It.IsAny<IEnumerable<FundHistoryRecord>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task IngestChartDataAsync_SingleSlotWithValidData_PersistsRecords()
    {
        // Arrange
        const string chartJson = """
            {
              "id": "1",
              "dataSerie": [
                { "x": 1771369200000, "y": 457.83 },
                { "x": 1771455600000, "y": 460.10 }
              ]
            }
            """;

        var pageData = new AboutFundPageData
        {
            OrderBookId = _fixture.Create<OrderBookId>(),
            Chart1Month = AboutFundFetchSlot.Succeeded(chartJson)
        };
        var isinId = _fixture.Create<IsinId>();
        List<FundHistoryRecord> capturedRecords = [];

        _repositoryMock
            .Setup(r => r.AddRangeIfNotExistsAsync(
                It.IsAny<IEnumerable<FundHistoryRecord>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<FundHistoryRecord>, CancellationToken>(
                (records, _) => capturedRecords.AddRange(records))
            .ReturnsAsync((IEnumerable<FundHistoryRecord> records, CancellationToken _) =>
                records.Count());

        // Act
        var result = await _sut.IngestChartDataAsync(pageData, isinId);

        // Assert
        Assert.That(result, Is.EqualTo(2));
        Assert.That(capturedRecords, Has.Count.EqualTo(2));
        Assert.That(capturedRecords.Select(r => r.IsinId), Is.All.EqualTo(isinId));
        Assert.That(capturedRecords[0].Nav, Is.EqualTo(457.83m));
        Assert.That(capturedRecords[1].Nav, Is.EqualTo(460.10m));
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Malformed data resilience

    [Test]
    public async Task IngestChartDataAsync_MalformedYInSlot_SkipsBadPointsKeepsValid()
    {
        // Arrange — exact payload from the bug report
        const string chartJson = """
            {
              "id": "100",
              "dataSerie": [
                {
                  "x": 1770678000000,
                  "y": { "source": "465.0", "parsedValue": 465 }
                },
                {
                  "x": 1771369200000,
                  "y": 457.83
                }
              ],
              "name": "Fund with invalid data",
              "fromDate": "2026-01-19",
              "toDate": "2026-02-18"
            }
            """;

        var pageData = new AboutFundPageData
        {
            OrderBookId = _fixture.Create<OrderBookId>(),
            Chart1Month = AboutFundFetchSlot.Succeeded(chartJson)
        };
        var isinId = _fixture.Create<IsinId>();
        List<FundHistoryRecord> capturedRecords = [];

        _repositoryMock
            .Setup(r => r.AddRangeIfNotExistsAsync(
                It.IsAny<IEnumerable<FundHistoryRecord>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<FundHistoryRecord>, CancellationToken>(
                (records, _) => capturedRecords.AddRange(records))
            .ReturnsAsync((IEnumerable<FundHistoryRecord> records, CancellationToken _) =>
                records.Count());

        // Act
        var result = await _sut.IngestChartDataAsync(pageData, isinId);

        // Assert — malformed point skipped, valid point persisted
        Assert.That(result, Is.EqualTo(1));
        Assert.That(capturedRecords, Has.Count.EqualTo(1));
        Assert.That(capturedRecords[0].Nav, Is.EqualTo(457.83m));
        Assert.That(capturedRecords[0].IsinId, Is.EqualTo(isinId));
    }

    [Test]
    public async Task IngestChartDataAsync_CompletelyInvalidJson_ReturnsZero()
    {
        // Arrange
        var pageData = new AboutFundPageData
        {
            OrderBookId = _fixture.Create<OrderBookId>(),
            Chart1Month = AboutFundFetchSlot.Succeeded("not json at all {{{")
        };
        var isinId = _fixture.Create<IsinId>();

        // Act
        var result = await _sut.IngestChartDataAsync(pageData, isinId);

        // Assert
        Assert.That(result, Is.EqualTo(0));
        _repositoryMock.Verify(
            r => r.AddRangeIfNotExistsAsync(
                It.IsAny<IEnumerable<FundHistoryRecord>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Deduplication

    [Test]
    public async Task IngestChartDataAsync_DuplicateNavDatesAcrossSlots_DeduplicatesFirstWins()
    {
        // Arrange — same timestamp in both slots, different NAV values.
        // 1M slot is processed first, so its value (100.00) should win.
        const string chart1MJson = """
            {
              "id": "1m",
              "dataSerie": [{ "x": 1771369200000, "y": 100.00 }]
            }
            """;
        const string chart3MJson = """
            {
              "id": "3m",
              "dataSerie": [{ "x": 1771369200000, "y": 999.99 }]
            }
            """;

        var pageData = new AboutFundPageData
        {
            OrderBookId = _fixture.Create<OrderBookId>(),
            Chart1Month = AboutFundFetchSlot.Succeeded(chart1MJson),
            Chart3Months = AboutFundFetchSlot.Succeeded(chart3MJson)
        };
        var isinId = _fixture.Create<IsinId>();
        List<FundHistoryRecord> capturedRecords = [];

        _repositoryMock
            .Setup(r => r.AddRangeIfNotExistsAsync(
                It.IsAny<IEnumerable<FundHistoryRecord>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<FundHistoryRecord>, CancellationToken>(
                (records, _) => capturedRecords.AddRange(records))
            .ReturnsAsync((IEnumerable<FundHistoryRecord> records, CancellationToken _) =>
                records.Count());

        // Act
        var result = await _sut.IngestChartDataAsync(pageData, isinId);

        // Assert — first occurrence (1M slot, 100.00) wins over 3M slot (999.99)
        Assert.That(result, Is.EqualTo(1));
        Assert.That(capturedRecords, Has.Count.EqualTo(1));
        Assert.That(capturedRecords[0].Nav, Is.EqualTo(100.00m));
    }

    #endregion

    #region Repository interaction

    [Test]
    public async Task IngestChartDataAsync_ValidData_CallsAddThenSaveInOrder()
    {
        // Arrange
        const string chartJson = """
            {
              "id": "1",
              "dataSerie": [{ "x": 1771369200000, "y": 100.0 }]
            }
            """;

        var pageData = new AboutFundPageData
        {
            OrderBookId = _fixture.Create<OrderBookId>(),
            Chart1Month = AboutFundFetchSlot.Succeeded(chartJson)
        };
        var isinId = _fixture.Create<IsinId>();
        var callOrder = new List<string>();

        _repositoryMock
            .Setup(r => r.AddRangeIfNotExistsAsync(
                It.IsAny<IEnumerable<FundHistoryRecord>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddRange"))
            .ReturnsAsync(1);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Save"))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.IngestChartDataAsync(pageData, isinId);

        // Assert
        Assert.That(callOrder, Is.EqualTo(new[] { "AddRange", "Save" }));
    }

    [Test]
    public async Task IngestChartDataAsync_RepositoryReturnsPartialCount_ForwardsCount()
    {
        // Arrange — 2 records sent, but repository says only 1 was new
        const string chartJson = """
            {
              "id": "1",
              "dataSerie": [
                { "x": 1771369200000, "y": 100.0 },
                { "x": 1771455600000, "y": 200.0 }
              ]
            }
            """;

        var pageData = new AboutFundPageData
        {
            OrderBookId = _fixture.Create<OrderBookId>(),
            Chart1Month = AboutFundFetchSlot.Succeeded(chartJson)
        };
        var isinId = _fixture.Create<IsinId>();

        _repositoryMock
            .Setup(r => r.AddRangeIfNotExistsAsync(
                It.IsAny<IEnumerable<FundHistoryRecord>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // Only 1 of 2 was new

        // Act
        var result = await _sut.IngestChartDataAsync(pageData, isinId);

        // Assert
        Assert.That(result, Is.EqualTo(1));
    }

    #endregion
}
