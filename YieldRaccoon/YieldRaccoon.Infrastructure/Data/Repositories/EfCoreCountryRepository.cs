using Microsoft.EntityFrameworkCore;
using YieldRaccoon.Application.Repositories;
using YieldRaccoon.Domain.Entities;
using YieldRaccoon.Domain.ValueObjects;
using YieldRaccoon.Infrastructure.Data.Context;

namespace YieldRaccoon.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICountryRepository"/>.
/// </summary>
public class EfCoreCountryRepository : ICountryRepository
{
    private readonly YieldRaccoonDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreCountryRepository"/> class.
    /// </summary>
    public EfCoreCountryRepository(YieldRaccoonDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<Country> GetOrCreateAsync(string displayName, string? countryCode,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.Countries
            .FirstOrDefaultAsync(c => c.DisplayName == displayName, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            // Backfill code only when payload provides one and existing is null.
            if (existing.CountryCode is null && countryCode is not null)
                existing.CountryCode = countryCode;

            return existing;
        }

        var created = new Country
        {
            Id = CountryId.New(),
            DisplayName = displayName,
            CountryCode = countryCode
        };

        await _context.Countries.AddAsync(created, cancellationToken).ConfigureAwait(false);
        return created;
    }

    /// <inheritdoc />
    public async Task<Country?> FindByDisplayNameAsync(string displayName,
        CancellationToken cancellationToken = default)
    {
        return await _context.Countries
            .FirstOrDefaultAsync(c => c.DisplayName == displayName, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
