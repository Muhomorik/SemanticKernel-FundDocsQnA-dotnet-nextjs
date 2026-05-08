using Backend.API.ApplicationCore.DTOs;
using Backend.API.Domain.FundData.Interfaces;
using Backend.API.Domain.FundData.Models;
using Backend.API.Domain.FundData.ValueObjects;

namespace Backend.API.ApplicationCore.Services;

/// <summary>
/// Maps incoming API DTOs to domain entities and delegates to repositories.
/// </summary>
public class FundSyncService : IFundSyncService
{
    private readonly IFundProfileRepository _profileRepository;
    private readonly IFundHistoryRepository _historyRepository;
    private readonly ICountryRepository _countryRepository;
    private readonly ISectorRepository _sectorRepository;
    private readonly IFundCountryAllocationRepository _countryAllocRepository;
    private readonly IFundSectorAllocationRepository _sectorAllocRepository;
    private readonly ILogger<FundSyncService> _logger;

    public FundSyncService(
        IFundProfileRepository profileRepository,
        IFundHistoryRepository historyRepository,
        ICountryRepository countryRepository,
        ISectorRepository sectorRepository,
        IFundCountryAllocationRepository countryAllocRepository,
        IFundSectorAllocationRepository sectorAllocRepository,
        ILogger<FundSyncService> logger)
    {
        _profileRepository = profileRepository;
        _historyRepository = historyRepository;
        _countryRepository = countryRepository;
        _sectorRepository = sectorRepository;
        _countryAllocRepository = countryAllocRepository;
        _sectorAllocRepository = sectorAllocRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FundSyncResponse> SyncFromFundListAsync(
        FundListSyncRequest request, CancellationToken cancellationToken = default)
    {
        var profilesProcessed = 0;
        var historyRecords = new List<FundHistoryRecord>();

        foreach (var dto in request.Funds)
        {
            if (string.IsNullOrWhiteSpace(dto.Isin) || string.IsNullOrWhiteSpace(dto.Name))
            {
                _logger.LogWarning("Skipping fund with missing ISIN or Name");
                continue;
            }

            IsinId isinId;
            try
            {
                isinId = IsinId.Create(dto.Isin);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Skipping fund with invalid ISIN: {Isin}", dto.Isin);
                continue;
            }

            var profile = CreateProfile(dto, isinId);
            await _profileRepository.UpsertAsync(profile, cancellationToken);

            var historyRecord = CreateHistoryRecord(dto, isinId);
            if (historyRecord.NavDate != null)
            {
                historyRecords.Add(historyRecord);
            }

            profilesProcessed++;
        }

        // Upsert history records (overwrites existing daily snapshots by ISIN+NavDate)
        await _historyRepository.UpsertRangeAsync(historyRecords, cancellationToken);
        await _profileRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Fund list sync: {Profiles} profiles, {History} history records",
            profilesProcessed, historyRecords.Count);

        return new FundSyncResponse
        {
            Success = true,
            Message = $"Synced {profilesProcessed} fund profiles with {historyRecords.Count} history records.",
            ProfilesProcessed = profilesProcessed,
            HistoryRecordsInserted = historyRecords.Count
        };
    }

    /// <inheritdoc />
    public async Task<FundSyncResponse> SyncFromFundAboutAsync(
        FundAboutSyncRequest request, CancellationToken cancellationToken = default)
    {
        var dto = request.Profile;

        if (string.IsNullOrWhiteSpace(dto.Isin) || string.IsNullOrWhiteSpace(dto.Name))
        {
            return new FundSyncResponse
            {
                Success = false,
                Message = "Fund profile must have ISIN and Name."
            };
        }

        IsinId isinId;
        try
        {
            isinId = IsinId.Create(dto.Isin);
        }
        catch (ArgumentException ex)
        {
            return new FundSyncResponse
            {
                Success = false,
                Message = $"Invalid ISIN format: {ex.Message}"
            };
        }

        var profile = CreateProfile(dto, isinId);

        // About endpoint never updates timestamps — null them out so the repository's
        // null-guard preserves existing values. Only /api/funds/list may update timestamps.
        profile.CrawlerLastUpdatedAt = null;
        profile.AboutFundLastVisitedAt = null;

        await _profileRepository.UpsertAsync(profile, cancellationToken);

        // Build chart history records (Nav + NavDate only)
        var historyRecords = new List<FundHistoryRecord>();
        foreach (var point in request.HistoryRecords)
        {
            var navDate = ParseDateOnly(point.NavDate);
            if (navDate == null || point.Nav == null) continue;

            IsinId pointIsinId;
            try
            {
                pointIsinId = IsinId.Create(point.Isin);
            }
            catch (ArgumentException)
            {
                continue;
            }

            historyRecords.Add(new FundHistoryRecord
            {
                IsinId = pointIsinId,
                Nav = point.Nav,
                NavDate = navDate
            });
        }

        // Insert-only: skip existing (ISIN, NavDate) pairs since chart data is immutable
        await _historyRepository.InsertIfNotExistsRangeAsync(historyRecords, cancellationToken);
        await _profileRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Fund about sync for {Isin}: {History} history records",
            dto.Isin, historyRecords.Count);

        return new FundSyncResponse
        {
            Success = true,
            Message = $"Synced profile for {dto.Isin} with {historyRecords.Count} chart history records.",
            ProfilesProcessed = 1,
            HistoryRecordsInserted = historyRecords.Count
        };
    }

    /// <inheritdoc />
    public async Task<FundSyncResponse> SyncFullHistoryAsync(
        FundFullHistorySyncRequest request, CancellationToken cancellationToken = default)
    {
        var dto = request.Profile;

        if (string.IsNullOrWhiteSpace(dto.Isin) || string.IsNullOrWhiteSpace(dto.Name))
        {
            return new FundSyncResponse
            {
                Success = false,
                Message = "Fund profile must have ISIN and Name."
            };
        }

        IsinId isinId;
        try
        {
            isinId = IsinId.Create(dto.Isin);
        }
        catch (ArgumentException ex)
        {
            return new FundSyncResponse
            {
                Success = false,
                Message = $"Invalid ISIN format: {ex.Message}"
            };
        }

        // Guarantee FK exists — never overwrite existing profile data
        var profile = CreateProfileFromMetadata(dto, isinId);
        await _profileRepository.InsertIfNotExistsAsync(profile, cancellationToken);

        // Map and upsert history records using sparse semantics
        var historyRecords = new List<FundHistoryRecord>();
        foreach (var record in request.HistoryRecords)
        {
            var navDate = ParseDateOnly(record.NavDate);
            if (navDate == null) continue;

            IsinId recordIsinId;
            try
            {
                recordIsinId = IsinId.Create(record.Isin);
            }
            catch (ArgumentException)
            {
                continue;
            }

            historyRecords.Add(new FundHistoryRecord
            {
                IsinId = recordIsinId,
                Nav = record.Nav,
                NavDate = navDate,
                Capital = record.Capital,
                NumberOfOwners = record.NumberOfOwners,
                Risk = record.Risk,
                SharpeRatio = record.SharpeRatio,
                StandardDeviation = record.StandardDeviation
            });
        }

        await _historyRepository.UpsertSparseRangeAsync(historyRecords, cancellationToken);
        await _profileRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Full history sync for {Isin}: {History} history records",
            dto.Isin, historyRecords.Count);

        return new FundSyncResponse
        {
            Success = true,
            Message = $"Full history sync for {dto.Isin}: {historyRecords.Count} history records processed.",
            ProfilesProcessed = 1,
            HistoryRecordsInserted = historyRecords.Count
        };
    }

    private static FundProfile CreateProfile(ApiFundDto dto, IsinId isinId)
    {
        return new FundProfile
        {
            Id = isinId,
            Name = dto.Name,
            OrderbookId = dto.OrderbookId,
            Category = dto.Category,
            CompanyName = dto.CompanyName,
            FundType = dto.FundType,
            IsIndexFund = dto.IsIndexFund,
            CurrencyCode = dto.CurrencyCode,
            ManagedType = dto.ManagedType,
            StartDate = ParseDateOnly(dto.StartDate),
            Buyable = dto.Buyable,
            HasCashDividends = dto.HasCashDividends,
            HasCurrencyExchangeFee = dto.HasCurrencyExchangeFee,
            RecommendedHoldingPeriod = dto.RecommendedHoldingPeriod,
            Description = dto.Description,
            ManagementFee = dto.ManagementFee,
            TotalFee = dto.TotalFee,
            TransactionFee = dto.TransactionFee,
            OngoingFee = dto.OngoingFee,
            MinimumBuy = dto.MinimumBuy,
            Capital = dto.Capital,
            NumberOfOwners = dto.NumberOfOwners,
            Rating = dto.Rating,
            Risk = dto.Risk,
            SharpeRatio = dto.SharpeRatio,
            StandardDeviation = dto.StandardDeviation,
            SustainabilityLevel = dto.SustainabilityLevel,
            SustainabilityRating = dto.SustainabilityRating,
            EsgScore = dto.EsgScore,
            EnvironmentalScore = dto.EnvironmentalScore,
            SocialScore = dto.SocialScore,
            GovernanceScore = dto.GovernanceScore,
            LowCarbon = dto.LowCarbon,
            EuArticleType = dto.EuArticleType,
            FirstSeenAt = ParseDateTimeOffset(dto.FirstSeenAt) ?? DateTimeOffset.UtcNow,
            CrawlerLastUpdatedAt = ParseDateTimeOffset(dto.CrawlerLastUpdatedAt),
            AboutFundLastVisitedAt = ParseDateTimeOffset(dto.AboutFundLastVisitedAt),
        };
    }

    private static FundProfile CreateProfileFromMetadata(ApiFundFullSyncProfileMetadataDto dto, IsinId isinId)
    {
        return new FundProfile
        {
            Id = isinId,
            Name = dto.Name,
            OrderbookId = dto.OrderbookId,
            Category = dto.Category,
            CompanyName = dto.CompanyName,
            FundType = dto.FundType,
            IsIndexFund = dto.IsIndexFund,
            CurrencyCode = dto.CurrencyCode,
            ManagedType = dto.ManagedType,
            StartDate = ParseDateOnly(dto.StartDate),
            Buyable = dto.Buyable,
            HasCashDividends = dto.HasCashDividends,
            HasCurrencyExchangeFee = dto.HasCurrencyExchangeFee,
            RecommendedHoldingPeriod = dto.RecommendedHoldingPeriod,
            Description = dto.Description,
            ManagementFee = dto.ManagementFee,
            TotalFee = dto.TotalFee,
            TransactionFee = dto.TransactionFee,
            OngoingFee = dto.OngoingFee,
            MinimumBuy = dto.MinimumBuy,
            Rating = dto.Rating,
            SustainabilityLevel = dto.SustainabilityLevel,
            SustainabilityRating = dto.SustainabilityRating,
            EsgScore = dto.EsgScore,
            EnvironmentalScore = dto.EnvironmentalScore,
            SocialScore = dto.SocialScore,
            GovernanceScore = dto.GovernanceScore,
            LowCarbon = dto.LowCarbon,
            EuArticleType = dto.EuArticleType,
            FirstSeenAt = ParseDateTimeOffset(dto.FirstSeenAt) ?? DateTimeOffset.UtcNow,
            CrawlerLastUpdatedAt = ParseDateTimeOffset(dto.CrawlerLastUpdatedAt),
            AboutFundLastVisitedAt = ParseDateTimeOffset(dto.AboutFundLastVisitedAt),
        };
    }

    private static FundHistoryRecord CreateHistoryRecord(ApiFundDto dto, IsinId isinId)
    {
        return new FundHistoryRecord
        {
            IsinId = isinId,
            Nav = dto.Nav,
            NavDate = ParseDateOnly(dto.NavDate),
            Capital = dto.Capital,
            NumberOfOwners = dto.NumberOfOwners,
            Risk = dto.Risk,
            SharpeRatio = dto.SharpeRatio,
            StandardDeviation = dto.StandardDeviation
        };
    }

    private static DateOnly? ParseDateOnly(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString)) return null;
        return DateOnly.TryParse(dateString, out var date) ? date : null;
    }

    private static DateTimeOffset? ParseDateTimeOffset(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString)) return null;
        return DateTimeOffset.TryParse(dateString, out var dto) ? dto : null;
    }

    /// <inheritdoc />
    public async Task<FundSyncResponse> SyncPortfolioAllocationsAsync(
        FundPortfolioAllocationsSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        IsinId isinId;
        try
        {
            isinId = IsinId.Create(request.Isin);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid ISIN in portfolio-allocations sync: {Isin}", request.Isin);
            return new FundSyncResponse
            {
                Success = false,
                Message = $"Invalid ISIN: {request.Isin}"
            };
        }

        var countryRows = await SyncCountryAllocationsAsync(isinId, request.Countries, cancellationToken);
        var sectorRows = await SyncSectorAllocationsAsync(isinId, request.Sectors, cancellationToken);

        await _countryAllocRepository.SaveChangesAsync(cancellationToken);

        var total = countryRows + sectorRows;
        _logger.LogInformation("Portfolio-allocations sync for {Isin}: {Countries} country rows + {Sectors} sector rows touched",
            request.Isin, countryRows, sectorRows);

        return new FundSyncResponse
        {
            Success = true,
            Message = $"Synced allocations for {request.Isin}: {countryRows} country rows + {sectorRows} sector rows touched.",
            ProfilesProcessed = 0,
            HistoryRecordsInserted = total
        };
    }

    private async Task<int> SyncCountryAllocationsAsync(
        IsinId isinId,
        IReadOnlyList<ApiCountryAllocationDto> items,
        CancellationToken ct)
    {
        var desired = new Dictionary<CountryId, decimal>();
        foreach (var item in items)
        {
            var country = await _countryRepository.GetOrCreateAsync(item.DisplayName, item.CountryCode, ct);
            desired[country.Id] = (decimal)item.Percentage;
        }

        var existing = (await _countryAllocRepository.GetByFundAsync(isinId, ct))
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
                await _countryAllocRepository.AddAsync(new FundCountryAllocation
                {
                    Id = FundCountryAllocationId.New(),
                    IsinId = isinId,
                    CountryId = countryId,
                    Percentage = pct
                }, ct);
                inserted++;
            }
        }

        if (toDelete.Count > 0)
            await _countryAllocRepository.RemoveRangeAsync(toDelete, ct);

        return inserted + updated + toDelete.Count;
    }

    private async Task<int> SyncSectorAllocationsAsync(
        IsinId isinId,
        IReadOnlyList<ApiSectorAllocationDto> items,
        CancellationToken ct)
    {
        var desired = new Dictionary<SectorId, decimal>();
        foreach (var item in items)
        {
            var sector = await _sectorRepository.GetOrCreateAsync(item.DisplayName, ct);
            desired[sector.Id] = (decimal)item.Percentage;
        }

        var existing = (await _sectorAllocRepository.GetByFundAsync(isinId, ct))
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
                await _sectorAllocRepository.AddAsync(new FundSectorAllocation
                {
                    Id = FundSectorAllocationId.New(),
                    IsinId = isinId,
                    SectorId = sectorId,
                    Percentage = pct
                }, ct);
                inserted++;
            }
        }

        if (toDelete.Count > 0)
            await _sectorAllocRepository.RemoveRangeAsync(toDelete, ct);

        return inserted + updated + toDelete.Count;
    }
}
