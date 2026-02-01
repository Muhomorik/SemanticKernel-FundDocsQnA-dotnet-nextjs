using Microsoft.EntityFrameworkCore;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Infrastructure.Data.Context;

namespace YieldRaccoon.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFundProfileRepository"/>.
/// </summary>
/// <remarks>
/// Provides persistent storage for <see cref="FundProfile"/> entities using SQLite.
/// </remarks>
public class EfCoreFundProfileRepository : IFundProfileRepository
{
    private readonly YieldRaccoonDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreFundProfileRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EfCoreFundProfileRepository(YieldRaccoonDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task AddOrUpdateAsync(FundProfile fundProfile, CancellationToken cancellationToken = default)
    {
        var existing = await _context.FundProfiles.FindAsync(new object[] { fundProfile.Id }, cancellationToken);
        if (existing is null)
        {
            await _context.FundProfiles.AddAsync(fundProfile, cancellationToken);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(fundProfile);
        }
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
