namespace YieldRaccoon.Application.Models;

/// <summary>
/// Identifies a data slot on <see cref="AboutFundPageData"/> that receives
/// an intercepted HTTP response during a fund page visit.
/// </summary>
public enum AboutFundDataSlot
{
    Chart1Month,
    Chart3Months,
    ChartYearToDate,
    Chart1Year,
    Chart3Years,
    Chart5Years,
    ChartMax
}
