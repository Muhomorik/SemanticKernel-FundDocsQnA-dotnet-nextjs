using NLog;
using NUnit.Framework;
using YieldRaccoon.Wpf.Services;

namespace YieldRaccoon.Wpf.Tests.Services;

[TestFixture]
[TestOf(typeof(FundListResponseInterceptor))]
public class FundListResponseInterceptorTests
{
    private ILogger _logger;
    private FundListResponseInterceptor _sut;

    [SetUp]
    public void SetUp()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _sut = new FundListResponseInterceptor(_logger);
    }

    #region ShouldInterceptResponse

    [Test]
    public void ShouldInterceptResponse_FundGuideListUrl_ReturnsTrue()
    {
        // Arrange
        var uri = "https://www.example.com/_api/fund-guide/list?sortField=name&sortOrder=ASCENDING";

        // Act
        var result = FundListResponseInterceptor.ShouldInterceptResponse(uri);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldInterceptResponse_FundGuideListUrlMinimal_ReturnsTrue()
    {
        // Arrange
        var uri = "https://www.example.com/_api/fund-guide/list";

        // Act
        var result = FundListResponseInterceptor.ShouldInterceptResponse(uri);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldInterceptResponse_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var uri = "https://www.example.com/_API/FUND-GUIDE/LIST?page=1";

        // Act
        var result = FundListResponseInterceptor.ShouldInterceptResponse(uri);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldInterceptResponse_UnrelatedUrl_ReturnsFalse()
    {
        // Arrange
        var uri = "https://www.example.com/api/user/profile";

        // Act
        var result = FundListResponseInterceptor.ShouldInterceptResponse(uri);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ShouldInterceptResponse_ChartEndpoint_ReturnsFalse()
    {
        // Arrange
        var uri = "https://www.example.com/_api/fund-guide/chart/12345/one_year?raw=true";

        // Act
        var result = FundListResponseInterceptor.ShouldInterceptResponse(uri);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ShouldInterceptResponse_EmptyString_ReturnsFalse()
    {
        // Act
        var result = FundListResponseInterceptor.ShouldInterceptResponse(string.Empty);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region ParseFundData

    [Test]
    public void ParseFundData_ValidFundListJson_ReturnsParsedResponse()
    {
        // Arrange
        var json = """
            {
                "fundListViews": [
                    { "isin": "SE0001234567", "name": "Test Fund Alpha" },
                    { "isin": "SE0009876543", "name": "Test Fund Beta" }
                ]
            }
            """;

        // Act
        var result = _sut.ParseFundData(json);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Funds, Has.Count.EqualTo(2));
        Assert.That(result.Funds![0].Isin, Is.EqualTo("SE0001234567"));
        Assert.That(result.Funds[0].Name, Is.EqualTo("Test Fund Alpha"));
        Assert.That(result.Funds[1].Isin, Is.EqualTo("SE0009876543"));
    }

    [Test]
    public void ParseFundData_ValidArrayJson_WrapsInResponse()
    {
        // Arrange — raw array (no wrapper object)
        var json = """
            [
                { "isin": "SE0001111111", "name": "Array Fund" }
            ]
            """;

        // Act
        var result = _sut.ParseFundData(json);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Funds, Has.Count.EqualTo(1));
        Assert.That(result.Funds![0].Isin, Is.EqualTo("SE0001111111"));
        Assert.That(result.Funds[0].Name, Is.EqualTo("Array Fund"));
    }

    [Test]
    public void ParseFundData_ValidJsonWithPaginationFields_ParsesPagination()
    {
        // Arrange
        var json = """
            {
                "fundListViews": [
                    { "isin": "SE0001234567", "name": "Test Fund" }
                ],
                "totalCount": 1462,
                "currentCount": 20
            }
            """;

        // Act
        var result = _sut.ParseFundData(json);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TotalCount, Is.EqualTo(1462));
        Assert.That(result.CurrentCount, Is.EqualTo(20));
        Assert.That(result.HasMore, Is.True);
    }

    [Test]
    public void ParseFundData_EmptyFundList_ReturnsResponseWithEmptyList()
    {
        // Arrange
        var json = """{ "fundListViews": [] }""";

        // Act
        var result = _sut.ParseFundData(json);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Funds, Is.Empty);
    }

    [Test]
    public void ParseFundData_InvalidJson_ReturnsNull()
    {
        // Arrange
        var json = "not valid json {{{";

        // Act
        var result = _sut.ParseFundData(json);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseFundData_EmptyString_ReturnsNull()
    {
        // Act
        var result = _sut.ParseFundData(string.Empty);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseFundData_FundWithAllFields_ParsesCorrectly()
    {
        // Arrange
        var json = """
            {
                "fundListViews": [
                    {
                        "isin": "SE0001234567",
                        "name": "Complete Fund",
                        "orderbookId": "12345",
                        "rating": 4,
                        "risk": 5,
                        "managementFee": 0.45,
                        "category": "Sweden",
                        "companyName": "Fund Provider AB",
                        "nrOfOwners": 42000
                    }
                ]
            }
            """;

        // Act
        var result = _sut.ParseFundData(json);

        // Assert
        Assert.That(result, Is.Not.Null);
        var fund = result!.Funds![0];
        Assert.Multiple(() =>
        {
            Assert.That(fund.Isin, Is.EqualTo("SE0001234567"));
            Assert.That(fund.Name, Is.EqualTo("Complete Fund"));
            Assert.That(fund.OrderBookId, Is.EqualTo("12345"));
            Assert.That(fund.Rating, Is.EqualTo(4));
            Assert.That(fund.Risk, Is.EqualTo(5));
            Assert.That(fund.ManagementFee, Is.EqualTo(0.45m));
            Assert.That(fund.Category, Is.EqualTo("Sweden"));
            Assert.That(fund.CompanyName, Is.EqualTo("Fund Provider AB"));
            Assert.That(fund.NumberOfOwners, Is.EqualTo(42000));
        });
    }

    #endregion
}
