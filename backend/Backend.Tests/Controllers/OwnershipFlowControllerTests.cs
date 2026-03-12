using Backend.API.ApplicationCore.DTOs.OwnershipFlow;
using Backend.API.ApplicationCore.Services;
using Backend.API.Configuration;
using Backend.API.Controllers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using NUnit.Framework;

namespace Backend.Tests.Controllers;

[TestFixture]
[Category("Unit")]
[Category("Controller")]
public class OwnershipFlowControllerTests
{
    private Mock<IOwnershipFlowService> _serviceMock = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IOwnershipFlowService>();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static BackendOptions OptionsWithSql() => new()
    {
        EmbeddingsFilePath = "test.json",
        OpenAIApiKey = "test-key",
        OpenAIEmbeddingModel = "text-embedding-3-small",
        MemoryCollectionName = "test",
        AzureSqlConnectionString = "Server=test;Database=test;"
    };

    private static BackendOptions OptionsWithoutSql() => new()
    {
        EmbeddingsFilePath = "test.json",
        OpenAIApiKey = "test-key",
        OpenAIEmbeddingModel = "text-embedding-3-small",
        MemoryCollectionName = "test",
        AzureSqlConnectionString = null
    };

    private OwnershipFlowController CreateController(BackendOptions options)
    {
        var controller = new OwnershipFlowController(
            _serviceMock.Object,
            options,
            NullLogger<OwnershipFlowController>.Instance);

        // Required for ModelState to be non-null
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private static OwnershipFlowResponse EmptyFlowResponse(string label = "Feb 10 – 16") =>
        new(label, new OwnershipFlowGroup([], []), new OwnershipFlowGroup([], []));

    // ─── GetPeriods ───────────────────────────────────────────────────────────

    [Test]
    public void GetPeriods_SqlNotConfigured_Returns503()
    {
        var sut = CreateController(OptionsWithoutSql());

        var result = sut.GetPeriods();

        Assert.That(result.Result, Is.InstanceOf<ObjectResult>()
            .With.Property("StatusCode").EqualTo(StatusCodes.Status503ServiceUnavailable));
    }

    [Test]
    public void GetPeriods_SqlConfigured_DelegatesToServiceAndReturns200()
    {
        var periods = new OwnershipFlowPeriodsResponse(
            Weekly: [new TimePeriod("Feb 10 – 16", "2025-02-10", "2025-02-16")],
            Monthly: [new TimePeriod("1 month", "2025-01-10", "2025-02-10")]);

        _serviceMock.Setup(s => s.GetAvailablePeriods()).Returns(periods);

        var sut = CreateController(OptionsWithSql());
        var result = sut.GetPeriods();

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(periods));
        _serviceMock.Verify(s => s.GetAvailablePeriods(), Times.Once);
    }

    // ─── GetOwnershipFlow — date validation ──────────────────────────────────

    [Test]
    public async Task GetOwnershipFlow_FromEqualsTo_Returns400()
    {
        var sut = CreateController(OptionsWithSql());
        var date = new DateOnly(2025, 2, 10);

        var result = await sut.GetOwnershipFlow(date, date, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetOwnershipFlow_FromAfterTo_Returns400()
    {
        var sut = CreateController(OptionsWithSql());

        var result = await sut.GetOwnershipFlow(
            new DateOnly(2025, 2, 16),
            new DateOnly(2025, 2, 10),
            CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetOwnershipFlow_FromEqualsTo_ErrorMentionsFromBeforeTo()
    {
        var sut = CreateController(OptionsWithSql());
        var date = new DateOnly(2025, 2, 10);

        var result = await sut.GetOwnershipFlow(date, date, CancellationToken.None);

        var body = ((BadRequestObjectResult)result.Result!).Value?.ToString();
        Assert.That(body, Does.Contain("from").IgnoreCase.Or.Contain("earlier").IgnoreCase);
    }

    [Test]
    public async Task GetOwnershipFlow_RangeExceeds365Days_Returns400()
    {
        var sut = CreateController(OptionsWithSql());
        var from = new DateOnly(2024, 1, 1);
        var to = from.AddDays(366);

        var result = await sut.GetOwnershipFlow(from, to, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetOwnershipFlow_RangeExceeds365Days_ErrorMentions365()
    {
        var sut = CreateController(OptionsWithSql());
        var from = new DateOnly(2024, 1, 1);
        var to = from.AddDays(366);

        var result = await sut.GetOwnershipFlow(from, to, CancellationToken.None);

        var body = ((BadRequestObjectResult)result.Result!).Value?.ToString();
        Assert.That(body, Does.Contain("365"));
    }

    [Test]
    public async Task GetOwnershipFlow_Exactly365DayRange_IsAccepted()
    {
        var from = new DateOnly(2024, 3, 1);
        var to = from.AddDays(365);

        _serviceMock
            .Setup(s => s.GetOwnershipFlowAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyFlowResponse());

        var sut = CreateController(OptionsWithSql());
        var result = await sut.GetOwnershipFlow(from, to, CancellationToken.None);

        Assert.That(result.Result, Is.Not.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetOwnershipFlow_FromInFuture_Returns400()
    {
        var sut = CreateController(OptionsWithSql());
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));

        var result = await sut.GetOwnershipFlow(future, future.AddDays(7), CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetOwnershipFlow_FromInFuture_ErrorMentionsFromCannotBeFuture()
    {
        var sut = CreateController(OptionsWithSql());
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));

        var result = await sut.GetOwnershipFlow(future, future.AddDays(7), CancellationToken.None);

        var body = ((BadRequestObjectResult)result.Result!).Value?.ToString();
        Assert.That(body, Does.Contain("future").IgnoreCase.Or.Contain("from").IgnoreCase);
    }

    // ─── GetOwnershipFlow — Azure SQL guard ──────────────────────────────────

    [Test]
    public async Task GetOwnershipFlow_SqlNotConfigured_Returns503()
    {
        var sut = CreateController(OptionsWithoutSql());
        var from = new DateOnly(2025, 2, 10);
        var to = new DateOnly(2025, 2, 16);

        var result = await sut.GetOwnershipFlow(from, to, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<ObjectResult>()
            .With.Property("StatusCode").EqualTo(StatusCodes.Status503ServiceUnavailable));
    }

    [Test]
    public async Task GetOwnershipFlow_SqlNotConfigured_ServiceIsNotCalled()
    {
        var sut = CreateController(OptionsWithoutSql());

        await sut.GetOwnershipFlow(new DateOnly(2025, 2, 10), new DateOnly(2025, 2, 16), CancellationToken.None);

        _serviceMock.Verify(
            s => s.GetOwnershipFlowAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── GetOwnershipFlow — happy path ───────────────────────────────────────

    [Test]
    public async Task GetOwnershipFlow_ValidRequest_DelegatesToServiceAndReturns200()
    {
        var from = new DateOnly(2025, 2, 10);
        var to = new DateOnly(2025, 2, 16);
        var expected = EmptyFlowResponse("Feb 10 – 16");

        _serviceMock
            .Setup(s => s.GetOwnershipFlowAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = CreateController(OptionsWithSql());
        var result = await sut.GetOwnershipFlow(from, to, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.EqualTo(expected));
        _serviceMock.Verify(s => s.GetOwnershipFlowAsync(from, to, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetOwnershipFlow_ValidWeeklyPeriod_PassesCorrectDatesToService()
    {
        var from = new DateOnly(2025, 1, 20);
        var to = new DateOnly(2025, 1, 26);

        _serviceMock
            .Setup(s => s.GetOwnershipFlowAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyFlowResponse("Jan 20 – 26"));

        var sut = CreateController(OptionsWithSql());
        await sut.GetOwnershipFlow(from, to, CancellationToken.None);

        _serviceMock.Verify(s => s.GetOwnershipFlowAsync(
            It.Is<DateOnly>(d => d == from),
            It.Is<DateOnly>(d => d == to),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetOwnershipFlow_ValidMonthlyPeriod_PassesCorrectDatesToService()
    {
        var from = new DateOnly(2024, 12, 11);
        var to = new DateOnly(2025, 3, 11);

        _serviceMock
            .Setup(s => s.GetOwnershipFlowAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyFlowResponse("3 months"));

        var sut = CreateController(OptionsWithSql());
        await sut.GetOwnershipFlow(from, to, CancellationToken.None);

        _serviceMock.Verify(s => s.GetOwnershipFlowAsync(
            It.Is<DateOnly>(d => d == from),
            It.Is<DateOnly>(d => d == to),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── GetOwnershipFlow — error handling ───────────────────────────────────

    [Test]
    public async Task GetOwnershipFlow_ServiceThrowsException_Returns500()
    {
        var from = new DateOnly(2025, 2, 10);
        var to = new DateOnly(2025, 2, 16);

        _serviceMock
            .Setup(s => s.GetOwnershipFlowAsync(from, to, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection lost"));

        var sut = CreateController(OptionsWithSql());
        var result = await sut.GetOwnershipFlow(from, to, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<ObjectResult>()
            .With.Property("StatusCode").EqualTo(StatusCodes.Status500InternalServerError));
    }

    [Test]
    public async Task GetOwnershipFlow_ServiceThrowsException_DoesNotLeakExceptionMessage()
    {
        var from = new DateOnly(2025, 2, 10);
        var to = new DateOnly(2025, 2, 16);
        const string internalMessage = "Server=secret;Password=hunter2";

        _serviceMock
            .Setup(s => s.GetOwnershipFlowAsync(from, to, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(internalMessage));

        var sut = CreateController(OptionsWithSql());
        var result = await sut.GetOwnershipFlow(from, to, CancellationToken.None);

        var body = ((ObjectResult)result.Result!).Value?.ToString();
        Assert.That(body, Does.Not.Contain(internalMessage));
    }
}
