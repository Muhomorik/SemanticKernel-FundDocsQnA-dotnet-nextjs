using Backend.API.Domain.FundData.Interfaces;
using Backend.API.Domain.FundData.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Infrastructure.FundData.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFundProfileRepository"/>.
/// Upsert: find by ISIN, if exists update (preserving FirstSeenAt and AboutFundLastVisitedAt), else insert.
/// </summary>
public class EfCoreFundProfileRepository : IFundProfileRepository
{
    private readonly FundDataDbContext _context;

    public EfCoreFundProfileRepository(FundDataDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task UpsertAsync(FundProfile profile, CancellationToken cancellationToken = default)
    {
        var existing = await _context.FundProfiles
            .FirstOrDefaultAsync(p => p.Id == profile.Id, cancellationToken);

        if (existing == null)
        {
            _context.FundProfiles.Add(profile);
        }
        else
        {
            // Update all mutable fields, preserve immutable timestamps
            existing.Name = profile.Name;
            existing.OrderbookId = profile.OrderbookId;
            existing.Category = profile.Category;
            existing.CompanyName = profile.CompanyName;
            existing.FundType = profile.FundType;
            existing.IsIndexFund = profile.IsIndexFund;
            existing.CurrencyCode = profile.CurrencyCode;
            existing.ManagedType = profile.ManagedType;
            existing.StartDate = profile.StartDate;
            existing.Buyable = profile.Buyable;
            existing.HasCashDividends = profile.HasCashDividends;
            existing.HasCurrencyExchangeFee = profile.HasCurrencyExchangeFee;
            existing.RecommendedHoldingPeriod = profile.RecommendedHoldingPeriod;
            existing.ManagementFee = profile.ManagementFee;
            existing.TotalFee = profile.TotalFee;
            existing.TransactionFee = profile.TransactionFee;
            existing.OngoingFee = profile.OngoingFee;
            existing.MinimumBuy = profile.MinimumBuy;
            existing.Capital = profile.Capital;
            existing.NumberOfOwners = profile.NumberOfOwners;
            existing.Rating = profile.Rating;
            existing.Risk = profile.Risk;
            existing.SharpeRatio = profile.SharpeRatio;
            existing.StandardDeviation = profile.StandardDeviation;
            existing.SustainabilityLevel = profile.SustainabilityLevel;
            existing.SustainabilityRating = profile.SustainabilityRating;
            existing.EsgScore = profile.EsgScore;
            existing.EnvironmentalScore = profile.EnvironmentalScore;
            existing.SocialScore = profile.SocialScore;
            existing.GovernanceScore = profile.GovernanceScore;
            existing.LowCarbon = profile.LowCarbon;
            existing.EuArticleType = profile.EuArticleType;
            existing.CrawlerLastUpdatedAt = profile.CrawlerLastUpdatedAt;

            // Only update AboutFundLastVisitedAt if explicitly set (non-null) on the incoming profile
            if (profile.AboutFundLastVisitedAt != null)
            {
                existing.AboutFundLastVisitedAt = profile.AboutFundLastVisitedAt;
            }

            // FirstSeenAt is never overwritten — it stays as originally set
        }
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
