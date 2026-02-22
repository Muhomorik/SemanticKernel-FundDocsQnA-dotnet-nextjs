using Moq;
using YieldRaccoon.Application.Services;

namespace YieldRaccoon.Infrastructure.Tests.TestHelpers;

/// <summary>
/// Extension methods for setting up <see cref="Mock{IAboutFundPageInteractor}"/> defaults.
/// </summary>
public static class PageInteractorMockExtensions
{
    /// <summary>
    /// Configures all interaction methods to return <c>true</c> (element clicked).
    /// </summary>
    public static Mock<IAboutFundPageInteractor> SetupAllSucceed(
        this Mock<IAboutFundPageInteractor> mock)
    {
        mock.Setup(x => x.ActivateSekViewAsync()).ReturnsAsync(true);
        mock.Setup(x => x.SelectPeriod1MonthAsync()).ReturnsAsync(true);
        mock.Setup(x => x.SelectPeriod3MonthsAsync()).ReturnsAsync(true);
        mock.Setup(x => x.SelectPeriodYearToDateAsync()).ReturnsAsync(true);
        mock.Setup(x => x.SelectPeriod1YearAsync()).ReturnsAsync(true);
        mock.Setup(x => x.SelectPeriod3YearsAsync()).ReturnsAsync(true);
        mock.Setup(x => x.SelectPeriod5YearsAsync()).ReturnsAsync(true);
        mock.Setup(x => x.SelectPeriodMaxAsync()).ReturnsAsync(true);
        return mock;
    }
}
