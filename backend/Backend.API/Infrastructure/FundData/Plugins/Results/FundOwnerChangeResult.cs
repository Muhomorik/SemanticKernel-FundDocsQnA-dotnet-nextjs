namespace Backend.API.Infrastructure.FundData.Plugins.Results;

/// <summary>
/// Result record returned by <see cref="FundDataPlugin.GetFundsByOwnerChangeAsync"/>.
/// Shows ownership delta over a time window for a single fund.
/// </summary>
public record FundOwnerChangeResult(
    string Isin,
    string Name,
    string? Category,
    int StartOwners,
    int EndOwners,
    int OwnerChange,
    DateOnly StartDate,
    DateOnly EndDate);
