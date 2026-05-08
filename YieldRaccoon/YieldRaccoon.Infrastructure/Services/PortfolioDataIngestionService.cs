using System.Text.Json;
using NLog;
using YieldRaccoon.Application.Models;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Application.Services;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Data.Context;
using YieldRaccoon.Infrastructure.Models;


namespace YieldRaccoon.Infrastructure.Services;

/// <summary>
/// Ingests country and sector portfolio allocations into the persistence layer.
/// </summary>
/// <remarks>
/// <para>
/// Diff-based merge: insert new (fund, country/sector) pairs, update existing percentages,
/// delete pairs that disappeared from the latest payload. The whole operation runs inside
/// a single transaction so partial failures roll back cleanly.
/// </para>
/// <para>
/// Lookup rows in <see cref="Country"/> and <see cref="Sector"/> grow organically: the first
/// encounter inserts; subsequent encounters reuse by display name.
/// </para>
/// </remarks>
public class PortfolioDataIngestionService : IPortfolioDataIngestionService
{
    private readonly ILogger _logger;
    private readonly YieldRaccoonDbContext _context;
    private readonly IFundProfileRepository _profileRepository;
    private readonly ICountryRepository _countries;
    private readonly ISectorRepository _sectors;
    private readonly IFundCountryAllocationRepository _countryAllocs;
    private readonly IFundSectorAllocationRepository _sectorAllocs;

    /// <summary>
    /// Initializes a new instance of the <see cref="PortfolioDataIngestionService"/> class.
    /// </summary>
    public PortfolioDataIngestionService(
        ILogger logger,
        YieldRaccoonDbContext context,
        IFundProfileRepository profileRepository,
        ICountryRepository countries,
        ISectorRepository sectors,
        IFundCountryAllocationRepository countryAllocs,
        IFundSectorAllocationRepository sectorAllocs)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _countries = countries ?? throw new ArgumentNullException(nameof(countries));
        _sectors = sectors ?? throw new ArgumentNullException(nameof(sectors));
        _countryAllocs = countryAllocs ?? throw new ArgumentNullException(nameof(countryAllocs));
        _sectorAllocs = sectorAllocs ?? throw new ArgumentNullException(nameof(sectorAllocs));
    }

    /// <inheritdoc />
    public async Task<int> IngestPortfolioDataAsync(
        AboutFundPageData pageData,
        IsinId isinId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(pageData.PortfolioDataJson))
        {
            _logger.Debug("No portfolio-data JSON for {0} — skipping", pageData.OrderBookId);
            return 0;
        }

        // FK guard: allocations have FK to FundProfile.
        if (!await _profileRepository.ExistsByIsinAsync(isinId, cancellationToken))
        {
            _logger.Warn("No fund profile for ISIN {0} (OrderBookId: {1}) — skipping portfolio ingestion",
                isinId.Isin, pageData.OrderBookId);
            return 0;
        }

        PortfolioDataResponse? dto;
        try
        {
            dto = JsonSerializer.Deserialize<PortfolioDataResponse>(pageData.PortfolioDataJson);
        }
        catch (JsonException ex)
        {
            _logger.Warn(ex, "Failed to deserialize portfolio-data JSON for {0}", pageData.OrderBookId);
            return 0;
        }

        if (dto is null)
        {
            _logger.Warn("Portfolio-data JSON deserialized to null for {0}", pageData.OrderBookId);
            return 0;
        }

        var countryItems = dto.CountryChartData ?? Array.Empty<CountryChartDataItem>();
        var sectorItems = dto.SectorChartData ?? Array.Empty<SectorChartDataItem>();

        if (countryItems.Count == 0 && sectorItems.Count == 0)
        {
            _logger.Debug("Portfolio-data payload has no countries or sectors for {0}", pageData.OrderBookId);
            return 0;
        }

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var countryRowsTouched = await SyncCountryAllocationsAsync(isinId, countryItems, cancellationToken);
            var sectorRowsTouched = await SyncSectorAllocationsAsync(isinId, sectorItems, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

            var total = countryRowsTouched + sectorRowsTouched;
            _logger.Info("Portfolio ingestion for {0} (ISIN: {1}): {2} country rows + {3} sector rows touched",
                pageData.OrderBookId, isinId.Isin, countryRowsTouched, sectorRowsTouched);

            return total;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            _logger.Error(ex, "Portfolio ingestion failed for {0} (ISIN: {1}) — transaction rolled back",
                pageData.OrderBookId, isinId.Isin);
            return 0;
        }
    }

    private async Task<int> SyncCountryAllocationsAsync(
        IsinId isinId,
        IReadOnlyList<CountryChartDataItem> items,
        CancellationToken ct)
    {
        // Resolve every payload country to a (CountryId, Percentage) pair, creating lookup rows as needed.
        var desired = new Dictionary<CountryId, decimal>();
        foreach (var item in items)
        {
            var country = await _countries.GetOrCreateAsync(item.DisplayName, item.CountryCode, ct)
                .ConfigureAwait(false);
            desired[country.Id] = (decimal)item.Percentage;
        }

        var existing = (await _countryAllocs.GetByFundAsync(isinId, ct).ConfigureAwait(false))
            .ToDictionary(a => a.CountryId);

        var toDelete = existing.Where(kv => !desired.ContainsKey(kv.Key)).Select(kv => kv.Value).ToList();
        var inserted = 0;
        var updated = 0;

        foreach (var (countryId, pct) in desired)
        {
            if (existing.TryGetValue(countryId, out var row))
            {
                if (row.Percentage != pct)
                {
                    row.Percentage = pct;
                    updated++;
                }
            }
            else
            {
                await _countryAllocs.AddAsync(new FundCountryAllocation
                {
                    Id = FundCountryAllocationId.New(),
                    IsinId = isinId,
                    CountryId = countryId,
                    Percentage = pct
                }, ct).ConfigureAwait(false);
                inserted++;
            }
        }

        if (toDelete.Count > 0)
            await _countryAllocs.RemoveRangeAsync(toDelete, ct).ConfigureAwait(false);

        return inserted + updated + toDelete.Count;
    }

    private async Task<int> SyncSectorAllocationsAsync(
        IsinId isinId,
        IReadOnlyList<SectorChartDataItem> items,
        CancellationToken ct)
    {
        var desired = new Dictionary<SectorId, decimal>();
        foreach (var item in items)
        {
            var sector = await _sectors.GetOrCreateAsync(item.DisplayName, ct).ConfigureAwait(false);
            desired[sector.Id] = (decimal)item.Percentage;
        }

        var existing = (await _sectorAllocs.GetByFundAsync(isinId, ct).ConfigureAwait(false))
            .ToDictionary(a => a.SectorId);

        var toDelete = existing.Where(kv => !desired.ContainsKey(kv.Key)).Select(kv => kv.Value).ToList();
        var inserted = 0;
        var updated = 0;

        foreach (var (sectorId, pct) in desired)
        {
            if (existing.TryGetValue(sectorId, out var row))
            {
                if (row.Percentage != pct)
                {
                    row.Percentage = pct;
                    updated++;
                }
            }
            else
            {
                await _sectorAllocs.AddAsync(new FundSectorAllocation
                {
                    Id = FundSectorAllocationId.New(),
                    IsinId = isinId,
                    SectorId = sectorId,
                    Percentage = pct
                }, ct).ConfigureAwait(false);
                inserted++;
            }
        }

        if (toDelete.Count > 0)
            await _sectorAllocs.RemoveRangeAsync(toDelete, ct).ConfigureAwait(false);

        return inserted + updated + toDelete.Count;
    }
}
