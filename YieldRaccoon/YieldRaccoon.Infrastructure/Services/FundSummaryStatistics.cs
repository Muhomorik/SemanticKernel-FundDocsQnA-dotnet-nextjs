namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Per-bucket summary statistics computed from daily NAV data for a single fund over a single
/// bi-weekly time window. Field names use the <c>_2w</c> horizon suffix to disambiguate from
/// snapshot.csv's <c>_12w</c> / <c>_1y</c> rolling-horizon counterparts.
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
    double Return2wPct,
    double AnnVolatility2wPct,
    double MaxDrawdown2wPct,
    double CurrentDrawdownPct,
    double Sharpe2w,
    double BestDayPct,
    double WorstDayPct,
    double PctPositiveDays,
    double Skewness);
