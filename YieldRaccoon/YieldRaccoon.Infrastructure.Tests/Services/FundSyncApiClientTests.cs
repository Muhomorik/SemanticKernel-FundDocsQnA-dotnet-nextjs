using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using NLog;
using NUnit.Framework;
using YieldRaccoon.Application.DTOs.Api;
using YieldRaccoon.Infrastructure.Services;

namespace YieldRaccoon.Infrastructure.Tests.Services;

[TestFixture]
[TestOf(typeof(FundSyncApiClient))]
public class FundSyncApiClientTests
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
    public async Task SyncFundListAsync_SendsCorrectUrlAndHeaders()
    {
        // Arrange
        var request = new FundListSyncRequest
        {
            Funds = [new ApiFundDto { Isin = "SE0001234567", Name = "Test Fund" }]
        };
        var responseBody = new FundSyncResponse { Success = true, Message = "OK", ProfilesProcessed = 1 };

        SetupHandler(HttpStatusCode.OK, responseBody, expectedUrl: "https://test-api.example.com/api/funds/list");

        // Act
        var result = await _sut.SyncFundListAsync(request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.ProfilesProcessed, Is.EqualTo(1));
        VerifyRequestSent(HttpMethod.Post, "https://test-api.example.com/api/funds/list");
    }

    [Test]
    public async Task SyncFundListAsync_DeserializesResponse()
    {
        // Arrange
        var request = new FundListSyncRequest
        {
            Funds = [new ApiFundDto { Isin = "SE0001234567", Name = "Test" }]
        };
        var responseBody = new FundSyncResponse
        {
            Success = true, Message = "Processed", ProfilesProcessed = 5, HistoryRecordsInserted = 10
        };

        SetupHandler(HttpStatusCode.OK, responseBody);

        // Act
        var result = await _sut.SyncFundListAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("Processed"));
            Assert.That(result.ProfilesProcessed, Is.EqualTo(5));
            Assert.That(result.HistoryRecordsInserted, Is.EqualTo(10));
        });
    }

    [Test]
    public void SyncFundListAsync_Non2xx_ThrowsHttpRequestException()
    {
        // Arrange
        var request = new FundListSyncRequest
        {
            Funds = [new ApiFundDto { Isin = "SE0001234567", Name = "Test" }]
        };

        SetupHandler(HttpStatusCode.InternalServerError, content: "Internal Server Error");

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(
            () => _sut.SyncFundListAsync(request));
    }

    [Test]
    public async Task SyncFundAboutAsync_SendsCorrectUrlAndHeaders()
    {
        // Arrange
        var request = new FundAboutSyncRequest
        {
            Profile = new ApiFundDto { Isin = "SE0001234567", Name = "Test Fund" },
            HistoryRecords =
            [
                new ApiFundHistoryPointDto { Isin = "SE0001234567", Nav = 100.5m, NavDate = "2024-01-01" }
            ]
        };
        var responseBody = new FundSyncResponse
        {
            Success = true, Message = "OK", ProfilesProcessed = 1, HistoryRecordsInserted = 1
        };

        SetupHandler(HttpStatusCode.OK, responseBody, expectedUrl: "https://test-api.example.com/api/funds/about");

        // Act
        var result = await _sut.SyncFundAboutAsync(request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.HistoryRecordsInserted, Is.EqualTo(1));
        VerifyRequestSent(HttpMethod.Post, "https://test-api.example.com/api/funds/about");
    }

    #region Helpers

    private void SetupHandler(HttpStatusCode statusCode, object? responseBody = null, string? content = null,
        string? expectedUrl = null)
    {
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var response = new HttpResponseMessage(statusCode);
                if (responseBody != null)
                    response.Content = new StringContent(
                        JsonSerializer.Serialize(responseBody),
                        System.Text.Encoding.UTF8,
                        "application/json");
                else if (content != null)
                    response.Content = new StringContent(content);
                return response;
            });
    }

    private void VerifyRequestSent(HttpMethod method, string url)
    {
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == method &&
                req.RequestUri!.ToString() == url),
            ItExpr.IsAny<CancellationToken>());
    }

    #endregion
}
