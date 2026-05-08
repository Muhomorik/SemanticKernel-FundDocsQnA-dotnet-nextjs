namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Per-fund rolling-horizon snapshot at a single evaluation date. One row per fund in the snapshot CSV.
/// All eight metric fields may be <see cref="double.NaN"/> when the underlying NAV history is too short
/// for the horizon, or when the volatility guard suppresses an explosive Sharpe.
/// </summary>
internal sealed record FundSnapshotStatistics(
    string Isin,
    DateOnly AsOfDate,
    double Return12wCompoundPct,
    double AnnVolatility12wPct,
    double Sharpe12w,
    double MaxDrawdown12wPct,
    double Return1yCompoundPct,
    double AnnVolatility1yPct,
    double Sharpe1y,
    double MaxDrawdown1yPct);
