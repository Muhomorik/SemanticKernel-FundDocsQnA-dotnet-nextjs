namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Summary statistics computed from daily NAV data for a single fund over a single time window.
/// </summary>
internal sealed record FundSummaryStatistics(
    string Isin,
    string Name,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal FirstNav,
    decimal LastNav,
    decimal NavHigh,
    decimal NavLow,
    double TotalReturnPct,
    double AnnVolatility,
    double MaxDrawdownPct,
    double CurrentDrawdownPct,
    double SharpeRatio,
    double BestDayPct,
    double WorstDayPct,
    double PctPositiveDays,
    double Skewness);
