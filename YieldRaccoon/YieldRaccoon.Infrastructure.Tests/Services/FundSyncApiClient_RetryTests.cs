using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Moq;
using Moq.Protected;
using NLog;
using NUnit.Framework;
using YieldRaccoon.Application.DTOs.Api;
using YieldRaccoon.Application.Exceptions;
using YieldRaccoon.Infrastructure.Services;

namespace YieldRaccoon.Infrastructure.Tests.Services;

[TestFixture]
[TestOf(typeof(FundSyncApiClient))]
public class FundSyncApiClient_RetryTests
{
    private Mock<ILogger> _loggerMock = null!;
    private Mock<HttpMessageHandler> _handlerMock = null!;
    private HttpClient _httpClient = null!;
    private FundSyncApiClient _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger>();
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://test-api.example.com/")
        };
        _sut = new FundSyncApiClient(_loggerMock.Object, _httpClient);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }

    [Test]
    public async Task SyncFundListAsync_429ThenSuccess_RetriesAndReturnsResult()
    {
        // Arrange
        var request = new FundListSyncRequest
        {
            Funds = [new ApiFundDto { Isin = "SE0001234567", Name = "Test" }]
        };
        var successResponse = new FundSyncResponse { Success = true, Message = "OK", ProfilesProcessed = 1 };

        SetupHandlerSequence(
            CreateResponse(HttpStatusCode.TooManyRequests),
            CreateJsonResponse(HttpStatusCode.OK, successResponse));

        // Act
        var result = await _sut.SyncFundListAsync(request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.ProfilesProcessed, Is.EqualTo(1));
        VerifyRequestCount(2);
    }

    [Test]
    public async Task SyncFundAboutAsync_429ThenSuccess_RetriesAndReturnsResult()
    {
        // Arrange
        var request = new FundAboutSyncRequest
        {
            Profile = new ApiFundDto { Isin = "SE0001234567", Name = "Test" },
            HistoryRecords = []
        };
        var successResponse = new FundSyncResponse { Success = true, Message = "OK", HistoryRecordsInserted = 5 };

        SetupHandlerSequence(
            CreateResponse(HttpStatusCode.TooManyRequests),
            CreateJsonResponse(HttpStatusCode.OK, successResponse));

        // Act
        var result = await _sut.SyncFundAboutAsync(request);

        // Assert
        Assert.That(result.HistoryRecordsInserted, Is.EqualTo(5));
        VerifyRequestCount(2);
    }

    [Test]
    public void SyncFundListAsync_429ThreeTimes_ThrowsRateLimitedException()
    {
        // Arrange
        var request = new FundListSyncRequest
        {
            Funds = [new ApiFundDto { Isin = "SE0001234567", Name = "Test" }]
        };

        SetupHandlerSequence(
            CreateResponse(HttpStatusCode.TooManyRequests),
            CreateResponse(HttpStatusCode.TooManyRequests),
            CreateResponse(HttpStatusCode.TooManyRequests),
            CreateResponse(HttpStatusCode.TooManyRequests));

        // Act & Assert
        var ex = Assert.ThrowsAsync<RateLimitedException>(
            () => _sut.SyncFundListAsync(request));
        Assert.That(ex!.AttemptsExhausted, Is.EqualTo(3));
    }

    [Test]
    public void SyncFundListAsync_Non429Error_ThrowsHttpRequestException()
    {
        // Arrange
        var request = new FundListSyncRequest
        {
            Funds = [new ApiFundDto { Isin = "SE0001234567", Name = "Test" }]
        };

        SetupHandlerSequence(
            CreateResponse(HttpStatusCode.InternalServerError));

        // Act & Assert — non-429 errors throw immediately, no retry
        Assert.ThrowsAsync<HttpRequestException>(
            () => _sut.SyncFundListAsync(request));
        VerifyRequestCount(1);
    }

    [Test]
    public async Task SyncFundListAsync_429WithRetryAfterHeader_RespectsHeader()
    {
        // Arrange
        var request = new FundListSyncRequest
        {
            Funds = [new ApiFundDto { Isin = "SE0001234567", Name = "Test" }]
        };
        var successResponse = new FundSyncResponse { Success = true, Message = "OK", ProfilesProcessed = 1 };

        var rateLimitResponse = CreateResponse(HttpStatusCode.TooManyRequests);
        rateLimitResponse.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(1));

        SetupHandlerSequence(
            rateLimitResponse,
            CreateJsonResponse(HttpStatusCode.OK, successResponse));

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await _sut.SyncFundListAsync(request);
        sw.Stop();

        // Assert — waited ~1s (Retry-After) instead of 2s (default backoff)
        Assert.That(result.Success, Is.True);
        Assert.That(sw.Elapsed.TotalSeconds, Is.LessThan(1.8),
            "Should respect Retry-After: 1s instead of default 2s backoff");
    }

    [Test]
    public void SyncFundListAsync_429_SupportsCancellation()
    {
        // Arrange
        var request = new FundListSyncRequest
        {
            Funds = [new ApiFundDto { Isin = "SE0001234567", Name = "Test" }]
        };
        var cts = new CancellationTokenSource();

        SetupHandlerSequence(
            CreateResponse(HttpStatusCode.TooManyRequests),
            CreateResponse(HttpStatusCode.TooManyRequests));

        // Cancel after first retry starts waiting
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        Assert.ThrowsAsync<TaskCanceledException>(
            () => _sut.SyncFundListAsync(request, cts.Token));
    }

    #region Helpers

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode)
    {
        return new HttpResponseMessage(statusCode);
    }

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, object body)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body),
                System.Text.Encoding.UTF8,
                "application/json")
        };
    }

    private void SetupHandlerSequence(params HttpResponseMessage[] responses)
    {
        var setup = _handlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        foreach (var response in responses)
            setup.ReturnsAsync(response);
    }

    private void VerifyRequestCount(int expectedCount)
    {
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(expectedCount),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    #endregion
}
